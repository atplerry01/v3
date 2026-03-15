namespace Whycespace.Domain.Events.Governance;

public sealed record GovernanceDecisionEvaluatedEvent(
    Guid EventId,
    string ProposalId,
    decimal ApprovalPercentage,
    decimal ParticipationPercentage,
    string DecisionRule,
    DateTime EvaluatedAt);
