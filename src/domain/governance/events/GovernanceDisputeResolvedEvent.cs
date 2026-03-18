namespace Whycespace.Domain.Governance.Events;

public sealed record GovernanceDisputeResolvedEvent(
    Guid EventId,
    Guid DisputeId,
    string ResolutionOutcome,
    Guid ResolvedByGuardianId,
    DateTime ResolvedAt);
