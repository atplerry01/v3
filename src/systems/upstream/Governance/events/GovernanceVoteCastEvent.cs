namespace Whycespace.Systems.Upstream.Governance.Events;

public sealed record GovernanceVoteCastEvent(
    Guid EventId,
    string ProposalId,
    string VoterId,
    string VoteValue,
    DateTimeOffset Timestamp);
