namespace Whycespace.Domain.Events.Governance;

public sealed record GovernanceQuorumFailedEvent(
    Guid EventId,
    Guid ProposalId,
    decimal ParticipationPercentage,
    decimal ApprovalPercentage,
    string FailureReason,
    DateTime EvaluatedAt);
