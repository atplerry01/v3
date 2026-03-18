namespace Whycespace.Systems.Upstream.Governance.Models;

public sealed record GovernanceDecisionResult(
    bool Success,
    string ProposalId,
    DecisionOutcome DecisionOutcome,
    decimal ApprovalPercentage,
    decimal ParticipationPercentage,
    DecisionRule DecisionRule,
    string Message,
    DateTime ExecutedAt);
