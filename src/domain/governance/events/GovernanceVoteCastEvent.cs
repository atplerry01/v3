namespace Whycespace.Domain.Events.Governance;

public sealed record GovernanceVoteCastEvent(
    Guid EventId,
    string VoteId,
    string ProposalId,
    string GuardianId,
    string VoteDecision,
    int VoteWeight,
    DateTime CastAt,
    int EventVersion)
{
    public static GovernanceVoteCastEvent Create(
        string voteId,
        string proposalId,
        string guardianId,
        string voteDecision,
        int voteWeight)
        => new(Guid.NewGuid(), voteId, proposalId, guardianId, voteDecision, voteWeight, DateTime.UtcNow, 1);
}
