namespace Whycespace.Domain.Events.Governance;

public sealed record GovernanceDisputeRaisedEvent(
    Guid EventId,
    Guid DisputeId,
    Guid ProposalId,
    string DisputeType,
    Guid RaisedByGuardianId,
    string DisputeReason,
    DateTime RaisedAt);
