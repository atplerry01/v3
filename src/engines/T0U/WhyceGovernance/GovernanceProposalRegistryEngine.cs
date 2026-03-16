namespace Whycespace.Engines.T0U.WhyceGovernance;

using Whycespace.Systems.Upstream.Governance.Models;
using Whycespace.Systems.Upstream.Governance.Stores;

public sealed class GovernanceProposalRegistryEngine
{
    private readonly GovernanceProposalStore _proposalStore;
    private readonly GuardianRegistryStore _guardianStore;

    public GovernanceProposalRegistryEngine(
        GovernanceProposalStore proposalStore,
        GuardianRegistryStore guardianStore)
    {
        _proposalStore = proposalStore;
        _guardianStore = guardianStore;
    }

    public GovernanceProposal CreateProposal(
        string proposalId,
        string title,
        string description,
        ProposalType type,
        string createdBy)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new InvalidOperationException("Proposal title is required.");

        if (!_guardianStore.Exists(createdBy))
            throw new KeyNotFoundException($"Guardian not found: {createdBy}");

        var proposal = new GovernanceProposal(
            proposalId,
            title,
            description,
            type,
            createdBy,
            DateTime.UtcNow,
            ProposalStatus.Draft);

        _proposalStore.Add(proposal);
        return proposal;
    }

    public GovernanceProposal GetProposal(string proposalId)
    {
        var proposal = _proposalStore.Get(proposalId)
            ?? throw new KeyNotFoundException($"Proposal not found: {proposalId}");
        return proposal;
    }

    public IReadOnlyList<GovernanceProposal> ListProposals()
    {
        return _proposalStore.ListAll();
    }
}
