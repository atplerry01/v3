namespace Whycespace.Domain.Events.Governance;

public sealed record GovernanceQuorumMetEvent(
    Guid EventId,
    Guid ProposalId,
    decimal ParticipationPercentage,
    decimal ApprovalPercentage,
    DateTime EvaluatedAt);
