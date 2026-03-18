namespace Whycespace.Domain.Governance.Events;

public sealed record GovernanceDisputeRaisedEvent(
    Guid EventId,
    Guid DisputeId,
    Guid ProposalId,
    string DisputeType,
    Guid RaisedByGuardianId,
    string DisputeReason,
    DateTime RaisedAt);
