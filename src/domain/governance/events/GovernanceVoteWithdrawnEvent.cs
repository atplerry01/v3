namespace Whycespace.Domain.Governance.Events;

public sealed record GovernanceVoteWithdrawnEvent(
    Guid EventId,
    string VoteId,
    string ProposalId,
    string GuardianId,
    string Reason,
    DateTime WithdrawnAt,
    int EventVersion)
{
    public static GovernanceVoteWithdrawnEvent Create(
        string voteId,
        string proposalId,
        string guardianId,
        string reason)
        => new(Guid.NewGuid(), voteId, proposalId, guardianId, reason, DateTime.UtcNow, 1);
}
