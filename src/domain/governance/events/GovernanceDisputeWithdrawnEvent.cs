namespace Whycespace.Domain.Events.Governance;

public sealed record GovernanceDisputeWithdrawnEvent(
    Guid EventId,
    Guid DisputeId,
    Guid WithdrawnByGuardianId,
    string Reason,
    DateTime WithdrawnAt);
