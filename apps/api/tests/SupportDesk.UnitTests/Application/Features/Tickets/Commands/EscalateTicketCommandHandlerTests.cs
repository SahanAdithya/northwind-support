using Microsoft.Extensions.Options;
using SupportDesk.Application.Abstractions;
using SupportDesk.Application.Contracts.Tickets;
using SupportDesk.Application.Exceptions;
using SupportDesk.Application.Features.Tickets.Commands.EscalateTicket;
using SupportDesk.Application.Features.Tickets.Queries.GetTicket;
using SupportDesk.Application.Features.Tickets.Triage;
using SupportDesk.Domain.Aggregates.Categories;
using SupportDesk.Domain.Aggregates.Customers;
using SupportDesk.Domain.Aggregates.Tickets;
using SupportDesk.Domain.Repositories;
using SupportDesk.UnitTests.TestDoubles;

namespace SupportDesk.UnitTests.Application.Features.Tickets.Commands;

public sealed class EscalateTicketCommandHandlerTests
{
    private const string Reason = "Customer impact has worsened; please prioritize this request.";

    [Fact]
    public async Task Escalating_KeepsEligibleOwnerResetsPremiumDueFromNowAndWritesHistory()
    {
        var context = new Fixture();
        var ticket = context.ExistingTicket(TicketPriority.Medium, ownerId: 4);
        var oldDue = ticket.DueAtUtc;
        context.Clock.Advance(TimeSpan.FromHours(3));
        context.SetCapacities(new(4, "Existing owner", true, 2, 6, true),
            new(7, "Other agent", true, 1, 6, true));

        var result = await context.Handler.HandleAsync(ticket.Id,
            new EscalateTicketRequest(Reason, "Alex Turner"), CancellationToken.None);

        Assert.Equal(TicketPriority.High, ticket.Priority);
        Assert.Equal(4, ticket.AssignedAgentId);
        Assert.Equal(context.Clock.UtcNow.AddHours(4), ticket.DueAtUtc); // Premium High = 8/2 h
        Assert.Equal(context.Clock.UtcNow, ticket.SlaStartedAtUtc);
        Assert.Equal(TicketPriority.Medium, result.Escalation.FromPriority);
        Assert.Equal(TicketPriority.High, result.Escalation.ToPriority);
        Assert.Equal(4, result.Escalation.FromAgentId);
        Assert.Equal(4, result.Escalation.ToAgentId);
        Assert.Equal(oldDue, result.Escalation.FromDueAtUtc);
        Assert.Equal(ticket.DueAtUtc, result.Escalation.ToDueAtUtc);
        Assert.Equal("Alex Turner", result.Escalation.EscalatedBy);
        Assert.Equal(Reason, result.Escalation.Reason);
        Assert.Equal(context.Clock.UtcNow, result.Escalation.EscalatedAtUtc);
        context.VerifySavedOnce();
    }

    [Fact]
    public async Task Escalating_ReassignsToFewestOpenQualifiedAgentWhenOwnerNoLongerEligible()
    {
        var context = new Fixture(new Category("Billing", requiresSpecialist: true, forcesCriticalPriority: false));
        var ticket = context.ExistingTicket(TicketPriority.Low, ownerId: 3);
        context.SetCapacities(
            new(3, "Inactive existing", false, 0, 8, true),
            new(10, "Full qualified", true, 8, 8, true),
            new(9, "Unqualified", true, 0, 8, false),
            new(11, "Qualified", true, 2, 8, true),
            new(12, "Qualified tie", true, 2, 8, true));

        await context.Handler.HandleAsync(ticket.Id,
            new EscalateTicketRequest(Reason, "Alex Turner"), CancellationToken.None);

        Assert.Equal(TicketPriority.Medium, ticket.Priority);
        Assert.Equal(11, ticket.AssignedAgentId);
        Assert.Equal(3, context.SavedHistory?.FromAgentId);
        Assert.Equal(11, context.SavedHistory?.ToAgentId);
        context.VerifySavedOnce();
    }

    [Fact]
    public async Task Escalating_WithoutEligibleAgentLeavesTicketUnassignedAndRecordsIt()
    {
        var context = new Fixture();
        var ticket = context.ExistingTicket(TicketPriority.Low, ownerId: null);
        context.SetCapacities(new(1, "Full", true, 8, 8, true),
            new(2, "Inactive", false, 0, 8, true));

        await context.Handler.HandleAsync(ticket.Id,
            new EscalateTicketRequest(Reason, "Alex Turner"), CancellationToken.None);

        Assert.Null(ticket.AssignedAgentId);
        Assert.Null(context.SavedHistory?.ToAgentId);
        context.VerifySavedOnce();
    }

    [Theory]
    [InlineData(TicketPriority.Critical, TicketStatus.New, "Critical")]
    [InlineData(TicketPriority.Medium, TicketStatus.Resolved, "Resolved")]
    [InlineData(TicketPriority.Medium, TicketStatus.Closed, "Resolved")]
    public async Task Escalating_CriticalOrTerminalTicketsReturnsConflictWithoutSaving(
        TicketPriority priority, TicketStatus status, string expectedMessage)
    {
        var context = new Fixture();
        var ticket = context.ExistingTicket(priority);
        if (status != TicketStatus.New) ticket.ChangeStatus(status, context.Clock.UtcNow);

        var error = await Assert.ThrowsAsync<ConflictException>(() => context.Handler.HandleAsync(
            ticket.Id, new EscalateTicketRequest(Reason, "Alex Turner"), CancellationToken.None));

        Assert.Contains(expectedMessage, error.Message);
        context.VerifyNothingSaved();
    }

    [Theory]
    [InlineData("bad", "Alex Turner")]
    [InlineData("valid reason", "")]
    public async Task Escalating_InvalidReasonOrActorReturnsValidationErrorWithoutSaving(
        string reason, string actor)
    {
        var context = new Fixture();
        var ticket = context.ExistingTicket(TicketPriority.Medium);

        await Assert.ThrowsAsync<ValidationException>(() => context.Handler.HandleAsync(
            ticket.Id, new EscalateTicketRequest(reason, actor), CancellationToken.None));
        context.VerifyNothingSaved();
    }

    [Fact]
    public async Task Escalating_UnknownTicketReturnsNotFound()
    {
        var context = new Fixture();
        await Assert.ThrowsAsync<NotFoundException>(() => context.Handler.HandleAsync(404,
            new EscalateTicketRequest(Reason, "Alex Turner"), CancellationToken.None));
        context.VerifyNothingSaved();
    }

    private sealed class Fixture
    {
        private readonly Mock<ITicketRepository> tickets = new();
        private readonly Mock<ICustomerRepository> customers = new();
        private readonly Mock<ICategoryRepository> categories = new();
        private readonly Mock<IAgentCapacityQueries> agents = new();
        private readonly Mock<ITicketEscalations> escalations = new();
        private readonly Mock<IUnitOfWork> unitOfWork = new();
        private readonly Mock<ITicketQueries> ticketQueries = new();

        public FixedClock Clock { get; } = new();
        public EscalateTicketCommandHandler Handler { get; }
        public TicketEscalation? SavedHistory { get; private set; }

        public Fixture(Category? category = null)
        {
            var currentCategory = category ?? new Category("General", false, false);
            customers.Setup(x => x.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Customer("Contoso", "support@example.com", null,
                    CustomerTier.Premium, Clock.UtcNow));
            categories.Setup(x => x.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(currentCategory);
            agents.Setup(x => x.GetAvailableAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Array.Empty<AgentCapacity>());
            escalations.Setup(x => x.AddAsync(It.IsAny<TicketEscalation>(), It.IsAny<CancellationToken>()))
                .Callback<TicketEscalation, CancellationToken>((history, _) => SavedHistory = history)
                .Returns(Task.CompletedTask);
            unitOfWork.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
            ticketQueries.Setup(x => x.GetDetailAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new TicketDetailDto { Id = 41, Reference = "TCK-0041" });
            Handler = new EscalateTicketCommandHandler(tickets.Object, customers.Object,
                categories.Object, agents.Object, escalations.Object, Options.Create(new SlaOptions()),
                Clock, unitOfWork.Object, new GetTicketQueryHandler(ticketQueries.Object));
        }

        public Ticket ExistingTicket(TicketPriority priority, int? ownerId = null)
        {
            var ticket = Ticket.Raise("TCK-0041", "Customer impacted", "A detailed issue description",
                1, 2, priority, Clock.UtcNow);
            ticket.ApplyTriage(priority, Clock.UtcNow.AddHours(24), ownerId, Clock.UtcNow);
            tickets.Setup(x => x.GetByIdAsync(ticket.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(ticket);
            return ticket;
        }

        public void SetCapacities(params AgentCapacity[] capacities) =>
            agents.Setup(x => x.GetAvailableAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(capacities);

        public void VerifySavedOnce()
        {
            escalations.Verify(x => x.AddAsync(It.IsAny<TicketEscalation>(),
                It.IsAny<CancellationToken>()), Times.Once());
            unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once());
        }

        public void VerifyNothingSaved()
        {
            escalations.Verify(x => x.AddAsync(It.IsAny<TicketEscalation>(),
                It.IsAny<CancellationToken>()), Times.Never());
            unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never());
        }
    }
}
