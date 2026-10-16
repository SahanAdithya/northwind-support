namespace SupportDesk.Application.Contracts.Tickets;

public sealed record TriageDecisionDto(string PriorityReason, string SlaReason, string AssignmentReason);
