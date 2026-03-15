namespace Whycespace.System.Upstream.Governance.Stores;

using global::System.Collections.Concurrent;
using Whycespace.System.Upstream.Governance.Models;

public sealed class GovernanceVoteStore
{
    private readonly ConcurrentDictionary<string, GovernanceVote> _votes = new();
    private readonly ConcurrentDictionary<string, HashSet<string>> _proposalVoters = new();

    public void Add(GovernanceVote vote)
    {
        var voters = _proposalVoters.GetOrAdd(vote.ProposalId, _ => new HashSet<string>());

        lock (voters)
        {
            if (!voters.Add(vote.GuardianId))
                throw new InvalidOperationException($"Guardian '{vote.GuardianId}' has already voted on proposal '{vote.ProposalId}'.");
        }

        _votes[vote.VoteId] = vote;
    }

    public bool Withdraw(string voteId, string proposalId, string guardianId)
    {
        if (!_votes.TryRemove(voteId, out _))
            return false;

        var voters = _proposalVoters.GetOrAdd(proposalId, _ => new HashSet<string>());
        lock (voters)
        {
            voters.Remove(guardianId);
        }

        return true;
    }

    public GovernanceVote? GetByGuardianAndProposal(string guardianId, string proposalId)
    {
        return _votes.Values.FirstOrDefault(v => v.GuardianId == guardianId && v.ProposalId == proposalId);
    }

    public bool HasVoted(string guardianId, string proposalId)
    {
        var voters = _proposalVoters.GetOrAdd(proposalId, _ => new HashSet<string>());
        lock (voters)
        {
            return voters.Contains(guardianId);
        }
    }

    public IReadOnlyList<GovernanceVote> GetByProposal(string proposalId)
    {
        return _votes.Values
            .Where(v => v.ProposalId == proposalId)
            .ToList();
    }
}
