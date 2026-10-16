using SupportDesk.Domain.Aggregates.Tickets;

namespace SupportDesk.Application.Features.Tickets.Triage;

public sealed record AgentCapacity(int Id, string Name, bool IsActive, int OpenCount, int Limit, bool IsSpecialist);

public static class TriagePolicy
{
    public static TicketPriority ChoosePriority(TicketPriority requested, bool forcesCritical) =>
        forcesCritical ? TicketPriority.Critical : requested;

    public static AgentCapacity? ChooseAgent(IEnumerable<AgentCapacity> candidates, bool requiresSpecialist) =>
        candidates.Where(a => a.IsActive && a.OpenCount < a.Limit &&
                              (!requiresSpecialist || a.IsSpecialist))
            .OrderBy(a => a.OpenCount).ThenBy(a => a.Id).FirstOrDefault();
}
