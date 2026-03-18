namespace Whycespace.Domain.Governance.Events;

public sealed record GovernanceDecisionApprovedEvent(
    Guid EventId,
    string ProposalId,
    string DecisionRule,
    decimal ApprovalPercentage,
    DateTime ApprovedAt);
