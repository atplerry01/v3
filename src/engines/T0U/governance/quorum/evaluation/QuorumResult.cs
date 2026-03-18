namespace Whycespace.Engines.T0U.Governance.Quorum.Evaluation;

public sealed record QuorumResult(
    bool Success,
    Guid ProposalId,
    decimal ParticipationPercentage,
    decimal ApprovalPercentage,
    bool QuorumMet,
    string Message,
    DateTime ExecutedAt);
