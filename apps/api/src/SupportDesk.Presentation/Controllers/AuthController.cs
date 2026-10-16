using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SupportDesk.Domain.Aggregates.Agents;
using SupportDesk.Infrastructure.Data;
using SupportDesk.Infrastructure.Services;

namespace SupportDesk.Presentation.Controllers;

public sealed record LoginRequest(string Email, string Password);
public sealed record CurrentAgent(int Id, string FullName, string Email);
public sealed record LoginResponse(string Token, DateTime ExpiresAtUtc, CurrentAgent User);

[ApiController]
[Route("api/auth")]
public sealed class AuthController(SupportDbContext db, IConfiguration config) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login(LoginRequest request, CancellationToken ct)
    {
        const string failure = "Invalid email or password.";
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            return Unauthorized(new ProblemDetails { Status = 401, Title = failure });
        var email = request.Email.Trim();
        var agent = await db.Set<Agent>().AsNoTracking()
            .FirstOrDefaultAsync(a => a.Email == email, ct);
        if (agent is null || !agent.IsActive || !PasswordService.Verify(request.Password, agent.PasswordHash))
            return Unauthorized(new ProblemDetails { Status = 401, Title = failure });
        var key = config["Jwt:Key"] ?? throw new InvalidOperationException("Jwt:Key is missing");
        var minutes = int.TryParse(config["Jwt:LifetimeMinutes"], out var configured) ? configured : 60;
        var expiry = DateTime.UtcNow.AddMinutes(minutes);
        var claims = new[] {
            new Claim(ClaimTypes.NameIdentifier, agent.Id.ToString()),
            new Claim(ClaimTypes.Name, agent.FullName),
            new Claim(ClaimTypes.Email, agent.Email)
        };
        var token = new JwtSecurityToken(
            issuer: config["Jwt:Issuer"], audience: config["Jwt:Audience"],
            claims: claims, expires: expiry,
            signingCredentials: new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
                SecurityAlgorithms.HmacSha256));
        return Ok(new LoginResponse(new JwtSecurityTokenHandler().WriteToken(token), expiry,
            new CurrentAgent(agent.Id, agent.FullName, agent.Email)));
    }

    [HttpGet("me")]
    public async Task<ActionResult<CurrentAgent>> Me(CancellationToken ct)
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id))
            return Unauthorized();
        var agent = await db.Set<Agent>().AsNoTracking()
            .Where(a => a.Id == id && a.IsActive)
            .Select(a => new CurrentAgent(a.Id, a.FullName, a.Email)).FirstOrDefaultAsync(ct);
        return agent is null ? Unauthorized() : Ok(agent);
    }
}
