namespace Whycespace.Domain.Events.Governance;

public sealed record GovernanceDecisionApprovedEvent(
    Guid EventId,
    string ProposalId,
    string DecisionRule,
    decimal ApprovalPercentage,
    DateTime ApprovedAt);
