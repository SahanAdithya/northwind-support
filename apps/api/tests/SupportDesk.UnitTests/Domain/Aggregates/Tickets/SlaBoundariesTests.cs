using SupportDesk.Domain.Aggregates.Tickets;

namespace SupportDesk.UnitTests.Domain.Aggregates.Tickets;

public class SlaBoundariesTests
{
    private readonly DateTime start = new(2026, 10, 16, 8, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void ExactQuarterRemainingIsAtRisk()
    {
        var due = start.AddHours(4);
        Assert.Equal(SlaStatus.WithinSla, SlaEvaluator.Evaluate(due, null, start.AddMinutes(179), start, 25));
        Assert.Equal(SlaStatus.AtRisk, SlaEvaluator.Evaluate(due, null, start.AddHours(3), start, 25));
        Assert.Equal(SlaStatus.AtRisk, SlaEvaluator.Evaluate(due, null, due, start, 25));
        Assert.Equal(SlaStatus.Breached, SlaEvaluator.Evaluate(due, null, due.AddTicks(1), start, 25));
        Assert.Equal(SlaStatus.Met, SlaEvaluator.Evaluate(due, due, due.AddDays(1), start, 25));
        Assert.Equal(SlaStatus.Breached, SlaEvaluator.Evaluate(due, due.AddTicks(1), due, start, 25));
        Assert.Equal(SlaStatus.NotApplicable, SlaEvaluator.Evaluate(null, null, start, start, 25));
    }

    [Fact]
    public void EscalationRaisesExactlyOneLevelAndResetsSlaClock()
    {
        var ticket = Ticket.Raise("TCK-1000", "A good test title", "A detailed test description", 1, 2,
            TicketPriority.Low, start);
        ticket.ApplyTriage(TicketPriority.Low, start.AddHours(72), 4, start);
        var escalatedAt = start.AddHours(4);
        ticket.Escalate(escalatedAt.AddHours(8), 4, escalatedAt);
        Assert.Equal(TicketPriority.Medium, ticket.Priority);
        Assert.Equal(escalatedAt, ticket.SlaStartedAtUtc);
        Assert.Equal(escalatedAt.AddHours(8), ticket.DueAtUtc);
    }
}
