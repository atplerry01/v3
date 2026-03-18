namespace Whycespace.Systems.Upstream.Governance.Stores;

using global::System.Collections.Concurrent;
using Whycespace.Systems.Upstream.Governance.Models;

public sealed class GovernanceProposalStore
{
    private readonly ConcurrentDictionary<string, GovernanceProposal> _proposals = new();

    public void Add(GovernanceProposal proposal)
    {
        if (!_proposals.TryAdd(proposal.ProposalId, proposal))
            throw new InvalidOperationException($"Proposal already exists: {proposal.ProposalId}");
    }

    public GovernanceProposal? Get(string proposalId)
    {
        _proposals.TryGetValue(proposalId, out var proposal);
        return proposal;
    }

    public void Update(GovernanceProposal proposal)
    {
        if (!_proposals.ContainsKey(proposal.ProposalId))
            throw new KeyNotFoundException($"Proposal not found: {proposal.ProposalId}");

        _proposals[proposal.ProposalId] = proposal;
    }

    public IReadOnlyList<GovernanceProposal> ListAll()
    {
        return _proposals.Values.ToList();
    }

    public IReadOnlyList<GovernanceProposal> ListByStatus(ProposalStatus status)
    {
        return _proposals.Values
            .Where(p => p.Status == status)
            .ToList();
    }
}
