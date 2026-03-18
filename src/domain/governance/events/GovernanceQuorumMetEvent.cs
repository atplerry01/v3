namespace Whycespace.Domain.Governance.Events;

public sealed record GovernanceQuorumMetEvent(
    Guid EventId,
    Guid ProposalId,
    decimal ParticipationPercentage,
    decimal ApprovalPercentage,
    DateTime EvaluatedAt);
