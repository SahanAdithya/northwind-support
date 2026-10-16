using SupportDesk.Application.Contracts.Tickets;
using SupportDesk.Domain.Aggregates.Tickets;

namespace SupportDesk.Application.Abstractions;

public interface ITicketEscalations
{
    Task AddAsync(TicketEscalation escalation, CancellationToken ct);
    Task<IReadOnlyList<TicketEscalationDto>> GetByTicketAsync(int ticketId, CancellationToken ct);
}
