using Microsoft.Extensions.Options;
using SupportDesk.Application.Abstractions;
using SupportDesk.Application.Contracts.Tickets;
using SupportDesk.Application.Exceptions;
using SupportDesk.Application.Features.Tickets.Queries.GetTicket;
using SupportDesk.Application.Features.Tickets.Triage;
using SupportDesk.Domain.Aggregates.Customers;
using SupportDesk.Domain.Aggregates.Tickets;
using SupportDesk.Domain.Repositories;

namespace SupportDesk.Application.Features.Tickets.Commands.EscalateTicket;

public sealed class EscalateTicketCommandHandler(
    ITicketRepository tickets, ICustomerRepository customers, ICategoryRepository categories,
    IAgentCapacityQueries agentCapacity, ITicketEscalations escalations,
    IOptions<SlaOptions> sla, IClock clock, IUnitOfWork unitOfWork, GetTicketQueryHandler getTicket)
{
    public async Task<EscalateTicketResponse> HandleAsync(int id, EscalateTicketRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Reason) || request.Reason.Trim().Length is < 5 or > 500 ||
            string.IsNullOrWhiteSpace(request.EscalatedBy) || request.EscalatedBy.Trim().Length > 100)
            throw new ValidationException(new Dictionary<string, string[]> {
                ["Reason"] = ["Reason must be 5–500 characters and actor is required."] });

        var ticket = await tickets.GetByIdAsync(id, ct) ?? throw new NotFoundException("Ticket", id);
        if (ticket.Priority == TicketPriority.Critical)
            throw new ConflictException("Critical tickets cannot be escalated.");
        if (!ticket.IsOpen)
            throw new ConflictException("Resolved or Closed tickets cannot be escalated.");

        var customer = await customers.GetByIdAsync(ticket.CustomerId, ct)
            ?? throw new NotFoundException("Customer", ticket.CustomerId);
        var category = await categories.GetByIdAsync(ticket.CategoryId, ct)
            ?? throw new NotFoundException("Category", ticket.CategoryId);
        var capacities = await agentCapacity.GetAvailableAsync(ticket.CategoryId, ct);
        var previousOwner = capacities.FirstOrDefault(a => a.Id == ticket.AssignedAgentId);
        var eligibleOwner = previousOwner is { IsActive: true } &&
            previousOwner.OpenCount < previousOwner.Limit &&
            (!category.RequiresSpecialist || previousOwner.IsSpecialist)
            ? previousOwner : TriagePolicy.ChooseAgent(capacities, category.RequiresSpecialist);
        var nextPriority = (TicketPriority)((int)ticket.Priority + 1);
        var now = clock.UtcNow;
        var due = now.Add(sla.Value.WindowFor(nextPriority, customer.Tier == CustomerTier.Premium));
        var history = new TicketEscalation(ticket.Id, ticket.Priority, nextPriority,
            ticket.AssignedAgentId, eligibleOwner?.Id, ticket.DueAtUtc, due,
            request.Reason, request.EscalatedBy!, now);
        ticket.Escalate(due, eligibleOwner?.Id, now);
        await escalations.AddAsync(history, ct);
        await unitOfWork.SaveChangesAsync(ct);
        return new EscalateTicketResponse(await getTicket.HandleAsync(id, ct),
            new TicketEscalationDto(history.Id, history.TicketId, history.FromPriority, history.ToPriority,
                history.FromAgentId, history.ToAgentId, history.FromDueAtUtc, history.ToDueAtUtc,
                history.Reason, history.EscalatedBy, history.EscalatedAtUtc));
    }
}
