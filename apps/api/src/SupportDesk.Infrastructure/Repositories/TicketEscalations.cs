using Microsoft.EntityFrameworkCore;
using SupportDesk.Application.Abstractions;
using SupportDesk.Application.Contracts.Tickets;
using SupportDesk.Domain.Aggregates.Tickets;
using SupportDesk.Infrastructure.Data;

namespace SupportDesk.Infrastructure.Repositories;

public sealed class TicketEscalations(SupportDbContext db) : ITicketEscalations
{
    public async Task AddAsync(TicketEscalation item, CancellationToken ct) =>
        await db.Set<TicketEscalation>().AddAsync(item, ct);

    public async Task<IReadOnlyList<TicketEscalationDto>> GetByTicketAsync(int ticketId, CancellationToken ct) =>
        await db.Set<TicketEscalation>().AsNoTracking()
            .Where(e => e.TicketId == ticketId)
            .OrderByDescending(e => e.EscalatedAtUtc).ThenByDescending(e => e.Id)
            .Select(e => new TicketEscalationDto(e.Id, e.TicketId, e.FromPriority, e.ToPriority,
                e.FromAgentId, e.ToAgentId, e.FromDueAtUtc, e.ToDueAtUtc,
                e.Reason, e.EscalatedBy, e.EscalatedAtUtc))
            .ToListAsync(ct);
}
