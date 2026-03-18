namespace Whycespace.Systems.Upstream.Governance.Events;

public sealed record GovernanceDisputeRaisedEvent(
    Guid EventId,
    string DisputeId,
    string ProposalId,
    string RaisedBy,
    string Reason,
    DateTimeOffset Timestamp);
