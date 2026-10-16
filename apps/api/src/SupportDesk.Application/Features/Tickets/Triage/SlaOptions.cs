using SupportDesk.Domain.Aggregates.Tickets;

namespace SupportDesk.Application.Features.Tickets.Triage;

public sealed class SlaOptions
{
    public Dictionary<string, double> BaseWindowHours { get; set; } = new()
    {
        ["Low"] = 72,
        ["Medium"] = 24,
        ["High"] = 8,
        ["Critical"] = 4
    };
    public double PremiumCustomerMultiplier { get; set; } = 0.5;
    public double MinimumWindowHours { get; set; } = 1;
    public double AtRiskThresholdPercent { get; set; } = 25;

    public TimeSpan WindowFor(TicketPriority priority, bool premium)
    {
        if (!BaseWindowHours.TryGetValue(priority.ToString(), out var hours) || hours <= 0)
            throw new InvalidOperationException($"Missing SLA window for {priority}.");
        var adjusted = premium ? hours * PremiumCustomerMultiplier : hours;
        return TimeSpan.FromHours(Math.Max(MinimumWindowHours, adjusted));
    }
}
