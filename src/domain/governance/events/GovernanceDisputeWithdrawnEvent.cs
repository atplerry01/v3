namespace Whycespace.Domain.Governance.Events;

public sealed record GovernanceDisputeWithdrawnEvent(
    Guid EventId,
    Guid DisputeId,
    Guid WithdrawnByGuardianId,
    string Reason,
    DateTime WithdrawnAt);
