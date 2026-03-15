namespace Whycespace.Domain.Events.Governance;

public sealed record GovernanceDecisionRejectedEvent(
    Guid EventId,
    string ProposalId,
    string DecisionRule,
    decimal ApprovalPercentage,
    DateTime RejectedAt);
