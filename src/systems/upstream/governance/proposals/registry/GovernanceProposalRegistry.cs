namespace Whycespace.Systems.Upstream.Governance.Proposals.Registry;

using Whycespace.Systems.Upstream.Governance.Proposals.Models;
using Whycespace.Systems.Upstream.Governance.Proposals.Stores;

public sealed class GovernanceProposalRegistry : IGovernanceProposalRegistry
{
    private readonly IGovernanceProposalStore _store;

    public GovernanceProposalRegistry(IGovernanceProposalStore store)
    {
        _store = store;
    }

    public void RegisterProposal(GovernanceProposalRecord proposal)
    {
        if (string.IsNullOrWhiteSpace(proposal.ProposalTitle))
            throw new ArgumentException("ProposalTitle must not be empty.");

        if (string.IsNullOrWhiteSpace(proposal.AuthorityDomain))
            throw new ArgumentException("AuthorityDomain must not be empty.");

        if (proposal.ProposedByGuardianId == Guid.Empty)
            throw new ArgumentException("ProposedByGuardianId must be a valid GUID.");

        if (!Enum.IsDefined(proposal.ProposalType))
            throw new ArgumentException("ProposalType must be a valid enum value.");

        if (_store.Get(proposal.ProposalId) is not null)
            throw new InvalidOperationException($"Proposal already exists: {proposal.ProposalId}");

        _store.Add(proposal);
    }

    public GovernanceProposalRecord? GetProposal(Guid proposalId)
    {
        return _store.Get(proposalId);
    }

    public IReadOnlyList<GovernanceProposalRecord> GetProposals()
    {
        return _store.ListAll();
    }

    public IReadOnlyList<GovernanceProposalRecord> GetProposalsByStatus(GovernanceProposalStatus status)
    {
        return _store.ListByStatus(status);
    }

    public IReadOnlyList<GovernanceProposalRecord> GetProposalsByType(GovernanceProposalType type)
    {
        return _store.ListByType(type);
    }

    public IReadOnlyList<GovernanceProposalRecord> GetProposalsByDomain(string domain)
    {
        return _store.ListByDomain(domain);
    }

    public void UpdateProposalStatus(Guid proposalId, GovernanceProposalStatus status)
    {
        var existing = _store.Get(proposalId)
            ?? throw new KeyNotFoundException($"Proposal not found: {proposalId}");

        var updated = existing with { ProposalStatus = status };
        _store.Update(updated);
    }
}
