namespace Whycespace.Engines.T0U.WhyceGovernance.Results;

public sealed record QuorumResult(
    bool Success,
    Guid ProposalId,
    decimal ParticipationPercentage,
    decimal ApprovalPercentage,
    bool QuorumMet,
    string Message,
    DateTime ExecutedAt);
