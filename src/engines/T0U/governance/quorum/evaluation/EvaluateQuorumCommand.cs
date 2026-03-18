namespace Whycespace.Engines.T0U.Governance.Quorum.Evaluation;

public sealed record EvaluateQuorumCommand(
    Guid CommandId,
    Guid ProposalId,
    int TotalEligibleGuardians,
    int VotesCast,
    int VotesApprove,
    int VotesReject,
    int VotesAbstain,
    decimal RequiredParticipationPercentage,
    decimal RequiredApprovalPercentage,
    DateTime Timestamp);
