using SupportDesk.Application.Features.Tickets.Triage;

namespace SupportDesk.Application.Abstractions;

public interface IAgentCapacityQueries
{
    Task<IReadOnlyList<AgentCapacity>> GetAvailableAsync(int categoryId, CancellationToken ct);
}
