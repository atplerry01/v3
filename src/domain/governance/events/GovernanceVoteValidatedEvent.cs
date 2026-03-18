namespace Whycespace.Domain.Governance.Events;

public sealed record GovernanceVoteValidatedEvent(
    Guid EventId,
    string ProposalId,
    string GuardianId,
    string VoteDecision,
    DateTime ValidatedAt,
    int EventVersion)
{
    public static GovernanceVoteValidatedEvent Create(
        string proposalId,
        string guardianId,
        string voteDecision)
        => new(Guid.NewGuid(), proposalId, guardianId, voteDecision, DateTime.UtcNow, 1);
}
