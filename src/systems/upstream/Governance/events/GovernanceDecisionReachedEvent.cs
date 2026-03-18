namespace Whycespace.Systems.Upstream.Governance.Events;

public sealed record GovernanceDecisionReachedEvent(
    Guid EventId,
    string ProposalId,
    string Outcome,
    int ApproveCount,
    int RejectCount,
    bool QuorumMet,
    DateTimeOffset Timestamp);
