namespace Whycespace.Domain.Governance.Events;

public sealed record GovernanceDecisionEvaluatedEvent(
    Guid EventId,
    string ProposalId,
    decimal ApprovalPercentage,
    decimal ParticipationPercentage,
    string DecisionRule,
    DateTime EvaluatedAt);
