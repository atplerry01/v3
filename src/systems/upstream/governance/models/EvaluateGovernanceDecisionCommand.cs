namespace Whycespace.Systems.Upstream.Governance.Models;

public sealed record EvaluateGovernanceDecisionCommand(
    Guid CommandId,
    string ProposalId,
    bool QuorumMet,
    int VotesApprove,
    int VotesReject,
    int VotesAbstain,
    decimal ApprovalPercentage,
    decimal ParticipationPercentage,
    DecisionRule DecisionRule,
    DateTime Timestamp);
