namespace Whycespace.Domain.Events.Governance;

public sealed record GovernanceQuorumEvaluatedEvent(
    Guid EventId,
    Guid ProposalId,
    int TotalEligibleGuardians,
    int VotesCast,
    int VotesApprove,
    int VotesReject,
    int VotesAbstain,
    decimal ParticipationPercentage,
    decimal ApprovalPercentage,
    DateTime EvaluatedAt);
