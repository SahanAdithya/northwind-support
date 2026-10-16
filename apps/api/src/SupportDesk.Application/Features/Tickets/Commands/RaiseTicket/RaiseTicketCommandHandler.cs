using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SupportDesk.Application.Abstractions;
using SupportDesk.Application.Contracts.Tickets;
using SupportDesk.Application.Exceptions;
using SupportDesk.Application.Features.Tickets.Queries.GetTicket;
using SupportDesk.Application.Features.Tickets.Triage;
using SupportDesk.Domain.Aggregates.Customers;
using SupportDesk.Domain.Aggregates.Tickets;
using SupportDesk.Domain.Repositories;

namespace SupportDesk.Application.Features.Tickets.Commands.RaiseTicket;

public sealed class RaiseTicketCommandHandler(
    ITicketRepository tickets, ICustomerRepository customers, ICategoryRepository categories,
    IAgentCapacityQueries agentCapacity, IOptions<SlaOptions> sla,
    IUnitOfWork unitOfWork, IClock clock, GetTicketQueryHandler getTicket,
    ILogger<RaiseTicketCommandHandler> logger)
{
    public async Task<TicketDetailDto> HandleAsync(CreateTicketRequest request, CancellationToken ct)
    {
        var customer = await customers.GetByIdAsync(request.CustomerId, ct)
            ?? throw new NotFoundException("Customer", request.CustomerId);
        var category = await categories.GetByIdAsync(request.CategoryId, ct)
            ?? throw new NotFoundException("Category", request.CategoryId);
        var now = clock.UtcNow;
        var requested = request.RequestedPriority ?? TicketPriority.Medium;
        var priority = TriagePolicy.ChoosePriority(requested, category.ForcesCriticalPriority);
        var premium = customer.Tier == CustomerTier.Premium;
        var window = sla.Value.WindowFor(priority, premium);
        var agents = await agentCapacity.GetAvailableAsync(category.Id, ct);
        var owner = TriagePolicy.ChooseAgent(agents, category.RequiresSpecialist);
        var ticket = Ticket.Raise(await tickets.NextReferenceAsync(ct),
            request.Title, request.Description, request.CustomerId, request.CategoryId, priority, now);
        ticket.ApplyTriage(priority, now.Add(window), owner?.Id, now);
        await tickets.AddAsync(ticket, ct);
        await unitOfWork.SaveChangesAsync(ct);
        logger.LogInformation("Created ticket {Reference}, triaged to {AgentId}.", ticket.Reference, owner?.Id);
        var dto = await getTicket.HandleAsync(ticket.Id, ct);
        dto.Triage = new TriageDecisionDto(
            category.ForcesCriticalPriority ? "Category requires Critical priority." : $"Requested {priority} priority.",
            $"{(premium ? "Premium" : "Standard")} SLA window of {window.TotalHours:g} hours.",
            owner is null ? "Unassigned: no active qualified agent is below capacity." :
            $"Assigned to {owner.Name} (fewest open tickets; deterministic ID tie-break).");
        return dto;
    }
}
