namespace Whycespace.Domain.Governance.Events;

public sealed record GovernanceQuorumFailedEvent(
    Guid EventId,
    Guid ProposalId,
    decimal ParticipationPercentage,
    decimal ApprovalPercentage,
    string FailureReason,
    DateTime EvaluatedAt);
