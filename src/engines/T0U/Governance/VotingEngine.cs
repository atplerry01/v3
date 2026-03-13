namespace Whycespace.Engines.T0U.Governance;

using Whycespace.System.Upstream.Governance.Models;
using Whycespace.System.Upstream.Governance.Stores;

public sealed class VotingEngine
{
    private readonly GovernanceVoteStore _voteStore;
    private readonly GovernanceProposalStore _proposalStore;
    private readonly GuardianRegistryStore _guardianStore;

    public VotingEngine(
        GovernanceVoteStore voteStore,
        GovernanceProposalStore proposalStore,
        GuardianRegistryStore guardianStore)
    {
        _voteStore = voteStore;
        _proposalStore = proposalStore;
        _guardianStore = guardianStore;
    }

    public GovernanceVote CastVote(string voteId, string proposalId, string guardianId, VoteType vote)
    {
        var proposal = _proposalStore.Get(proposalId)
            ?? throw new KeyNotFoundException($"Proposal not found: {proposalId}");

        if (proposal.Status != ProposalStatus.Voting)
            throw new InvalidOperationException($"Proposal is not in Voting status. Current status: {proposal.Status}");

        var guardian = _guardianStore.GetGuardian(guardianId)
            ?? throw new KeyNotFoundException($"Guardian not found: {guardianId}");

        if (guardian.Status != GuardianStatus.Active)
            throw new InvalidOperationException($"Inactive guardians cannot vote. Guardian status: {guardian.Status}");

        var governanceVote = new GovernanceVote(
            voteId,
            proposalId,
            guardianId,
            vote,
            DateTime.UtcNow);

        _voteStore.Add(governanceVote);
        return governanceVote;
    }

    public IReadOnlyList<GovernanceVote> GetVotes(string proposalId)
    {
        if (_proposalStore.Get(proposalId) is null)
            throw new KeyNotFoundException($"Proposal not found: {proposalId}");

        return _voteStore.GetByProposal(proposalId);
    }

    public VoteTally CountVotes(string proposalId)
    {
        if (_proposalStore.Get(proposalId) is null)
            throw new KeyNotFoundException($"Proposal not found: {proposalId}");

        var votes = _voteStore.GetByProposal(proposalId);

        return new VoteTally(
            votes.Count(v => v.Vote == VoteType.Approve),
            votes.Count(v => v.Vote == VoteType.Reject),
            votes.Count(v => v.Vote == VoteType.Abstain));
    }
}

public sealed record VoteTally(int Approve, int Reject, int Abstain)
{
    public int Total => Approve + Reject + Abstain;
}
