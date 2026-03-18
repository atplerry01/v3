namespace Whycespace.Domain.Governance.Events;

public sealed record GovernanceDecisionRejectedEvent(
    Guid EventId,
    string ProposalId,
    string DecisionRule,
    decimal ApprovalPercentage,
    DateTime RejectedAt);
