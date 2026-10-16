using Microsoft.EntityFrameworkCore;
using SupportDesk.Application.Abstractions;
using SupportDesk.Application.Features.Tickets.Triage;
using SupportDesk.Domain.Aggregates.Agents;
using SupportDesk.Domain.Aggregates.Tickets;
using SupportDesk.Infrastructure.Data;

namespace SupportDesk.Infrastructure.Queries;

public sealed class AgentCapacityQueries(SupportDbContext db) : IAgentCapacityQueries
{
    public async Task<IReadOnlyList<AgentCapacity>> GetAvailableAsync(int categoryId, CancellationToken ct) =>
        await db.Set<Agent>().AsNoTracking().Select(a => new AgentCapacity(
            a.Id, a.FullName, a.IsActive,
            db.Set<Ticket>().Count(t => t.AssignedAgentId == a.Id &&
                t.Status != TicketStatus.Resolved && t.Status != TicketStatus.Closed),
            a.MaxOpenTickets, a.Specializations.Any(s => s.CategoryId == categoryId)))
            .ToListAsync(ct);
}
