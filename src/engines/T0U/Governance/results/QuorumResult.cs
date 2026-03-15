namespace Whycespace.Engines.T0U.Governance.Results;

public sealed record QuorumResult(
    bool Success,
    Guid ProposalId,
    decimal ParticipationPercentage,
    decimal ApprovalPercentage,
    bool QuorumMet,
    string Message,
    DateTime ExecutedAt);
