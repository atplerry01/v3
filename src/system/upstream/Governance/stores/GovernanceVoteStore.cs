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

    public IReadOnlyList<GovernanceVote> GetByProposal(string proposalId)
    {
        return _votes.Values
            .Where(v => v.ProposalId == proposalId)
            .ToList();
    }
}
