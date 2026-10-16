namespace SupportDesk.Domain.Aggregates.Tickets;

/// <summary>SLA is derived at read time; timestamps are never used as a status column.</summary>
public static class SlaEvaluator
{
    public static SlaStatus Evaluate(DateTime? dueAtUtc, DateTime? resolvedAtUtc, DateTime nowUtc) =>
        Evaluate(dueAtUtc, resolvedAtUtc, nowUtc, nowUtc, 0);

    public static SlaStatus Evaluate(
        DateTime? dueAtUtc, DateTime? resolvedAtUtc, DateTime nowUtc,
        DateTime createdAtUtc, double atRiskThresholdPercent)
    {
        if (dueAtUtc is null) return SlaStatus.NotApplicable;
        if (resolvedAtUtc is not null)
            return resolvedAtUtc <= dueAtUtc ? SlaStatus.Met : SlaStatus.Breached;
        if (nowUtc > dueAtUtc) return SlaStatus.Breached;
        var total = dueAtUtc.Value - createdAtUtc;
        var remaining = dueAtUtc.Value - nowUtc;
        if (total > TimeSpan.Zero && remaining.TotalSeconds <=
            total.TotalSeconds * atRiskThresholdPercent / 100d)
            return SlaStatus.AtRisk;
        return SlaStatus.WithinSla;
    }
}
