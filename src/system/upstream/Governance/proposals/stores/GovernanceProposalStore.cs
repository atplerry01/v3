namespace Whycespace.System.Upstream.Governance.Proposals.Stores;

using global::System.Collections.Concurrent;
using Whycespace.System.Upstream.Governance.Proposals.Models;

public sealed class GovernanceProposalStore : IGovernanceProposalStore
{
    private readonly ConcurrentDictionary<Guid, GovernanceProposalRecord> _proposals = new();

    public void Add(GovernanceProposalRecord proposal)
    {
        if (!_proposals.TryAdd(proposal.ProposalId, proposal))
            throw new InvalidOperationException($"Proposal already exists: {proposal.ProposalId}");
    }

    public GovernanceProposalRecord? Get(Guid proposalId)
    {
        _proposals.TryGetValue(proposalId, out var proposal);
        return proposal;
    }

    public void Update(GovernanceProposalRecord proposal)
    {
        if (!_proposals.ContainsKey(proposal.ProposalId))
            throw new KeyNotFoundException($"Proposal not found: {proposal.ProposalId}");

        _proposals[proposal.ProposalId] = proposal;
    }

    public IReadOnlyList<GovernanceProposalRecord> ListAll()
    {
        return _proposals.Values.ToList();
    }

    public IReadOnlyList<GovernanceProposalRecord> ListByStatus(GovernanceProposalStatus status)
    {
        return _proposals.Values
            .Where(p => p.ProposalStatus == status)
            .ToList();
    }

    public IReadOnlyList<GovernanceProposalRecord> ListByType(GovernanceProposalType type)
    {
        return _proposals.Values
            .Where(p => p.ProposalType == type)
            .ToList();
    }

    public IReadOnlyList<GovernanceProposalRecord> ListByDomain(string domain)
    {
        return _proposals.Values
            .Where(p => p.AuthorityDomain == domain)
            .ToList();
    }

    public bool Exists(Guid proposalId)
    {
        return _proposals.ContainsKey(proposalId);
    }
}
