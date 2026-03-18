namespace Whycespace.Domain.Events.Governance;

public sealed record GovernanceDisputeResolvedEvent(
    Guid EventId,
    Guid DisputeId,
    string ResolutionOutcome,
    Guid ResolvedByGuardianId,
    DateTime ResolvedAt);
