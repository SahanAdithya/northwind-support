using SupportDesk.Application.Features.Tickets.Triage;
using SupportDesk.Domain.Aggregates.Tickets;

namespace SupportDesk.UnitTests.Application.Features.Tickets.Triage;

public class TriagePolicyTests
{
    [Theory]
    [InlineData(TicketPriority.Low, 72)]
    [InlineData(TicketPriority.Medium, 24)]
    [InlineData(TicketPriority.High, 8)]
    [InlineData(TicketPriority.Critical, 4)]
    public void StandardWindowMatchesConfiguredBase(TicketPriority priority, double hours) =>
        Assert.Equal(hours, new SlaOptions().WindowFor(priority, false).TotalHours);

    [Theory]
    [InlineData(TicketPriority.Low, 36)]
    [InlineData(TicketPriority.Medium, 12)]
    [InlineData(TicketPriority.High, 4)]
    [InlineData(TicketPriority.Critical, 2)]
    public void PremiumWindowIsHalf(TicketPriority priority, double hours) =>
        Assert.Equal(hours, new SlaOptions().WindowFor(priority, true).TotalHours);

    [Fact]
    public void FloorPreventsShorterThanOneHour()
    {
        var options = new SlaOptions { PremiumCustomerMultiplier = 0.01 };
        Assert.Equal(1, options.WindowFor(TicketPriority.Critical, true).TotalHours);
    }

    [Fact]
    public void ForcedCriticalOverridesReporter()
    {
        Assert.Equal(TicketPriority.Critical, TriagePolicy.ChoosePriority(TicketPriority.Low, true));
        Assert.Equal(TicketPriority.Low, TriagePolicy.ChoosePriority(TicketPriority.Low, false));
    }

    [Fact]
    public void PicksFewestOpenAndBreaksTiesOnId()
    {
        AgentCapacity[] agents = [
            new(4, "Fourth", true, 2, 10, true), new(1, "First", true, 2, 10, true),
            new(3, "Third", true, 1, 1, true), new(2, "Second", false, 0, 10, true)
        ];
        Assert.Equal(1, TriagePolicy.ChooseAgent(agents, true)?.Id);
    }

    [Fact]
    public void SpecialistRequired_ExcludesGeneralistsAndFullAgents()
    {
        AgentCapacity[] agents = [
            new(1, "Generalist", true, 0, 8, false),
            new(2, "Qualified but full", true, 8, 8, true),
            new(3, "Qualified", true, 3, 8, true)
        ];
        Assert.Equal(3, TriagePolicy.ChooseAgent(agents, true)?.Id);
    }

    [Fact]
    public void NoEligibleAgentIsNormal()
    {
        AgentCapacity[] agents = [new(2, "Full", true, 4, 4, true),
            new(1, "Inactive", false, 0, 10, true)];
        Assert.Null(TriagePolicy.ChooseAgent(agents, true));
    }
}
