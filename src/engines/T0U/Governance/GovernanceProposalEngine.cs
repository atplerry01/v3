namespace Whycespace.Engines.T0U.Governance;

using Whycespace.System.Upstream.Governance.Models;
using Whycespace.System.Upstream.Governance.Stores;

public sealed class GovernanceProposalEngine
{
    private readonly GovernanceProposalStore _proposalStore;

    public GovernanceProposalEngine(GovernanceProposalStore proposalStore)
    {
        _proposalStore = proposalStore;
    }

    public GovernanceProposal OpenProposal(string proposalId)
    {
        var proposal = GetOrThrow(proposalId);

        if (proposal.Status != ProposalStatus.Draft)
            throw new InvalidOperationException($"Proposal must be in Draft to open. Current status: {proposal.Status}");

        var updated = proposal with { Status = ProposalStatus.Open };
        _proposalStore.Update(updated);
        return updated;
    }

    public GovernanceProposal StartVoting(string proposalId)
    {
        var proposal = GetOrThrow(proposalId);

        if (proposal.Status != ProposalStatus.Open)
            throw new InvalidOperationException($"Proposal must be Open to start voting. Current status: {proposal.Status}");

        var updated = proposal with { Status = ProposalStatus.Voting };
        _proposalStore.Update(updated);
        return updated;
    }

    public GovernanceProposal CloseProposal(string proposalId)
    {
        var proposal = GetOrThrow(proposalId);

        if (proposal.Status == ProposalStatus.Closed)
            throw new InvalidOperationException("Proposal is already closed.");

        if (proposal.Status == ProposalStatus.Draft)
            throw new InvalidOperationException("Cannot close a Draft proposal. Open it first.");

        var updated = proposal with { Status = ProposalStatus.Closed };
        _proposalStore.Update(updated);
        return updated;
    }

    public GovernanceProposal RejectProposal(string proposalId)
    {
        var proposal = GetOrThrow(proposalId);

        if (proposal.Status != ProposalStatus.Voting)
            throw new InvalidOperationException($"Proposal must be in Voting to reject. Current status: {proposal.Status}");

        var updated = proposal with { Status = ProposalStatus.Rejected };
        _proposalStore.Update(updated);
        return updated;
    }

    private GovernanceProposal GetOrThrow(string proposalId)
    {
        return _proposalStore.Get(proposalId)
            ?? throw new KeyNotFoundException($"Proposal not found: {proposalId}");
    }
}
