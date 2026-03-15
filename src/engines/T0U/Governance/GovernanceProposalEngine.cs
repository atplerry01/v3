namespace Whycespace.Engines.T0U.Governance;

using Whycespace.Domain.Events.Governance;
using Whycespace.Engines.T0U.Governance.Commands;
using Whycespace.Engines.T0U.Governance.Results;
using Whycespace.System.Upstream.Governance.Models;
using Whycespace.System.Upstream.Governance.Stores;

public sealed class GovernanceProposalEngine
{
    private readonly GovernanceProposalStore _proposalStore;

    public GovernanceProposalEngine(GovernanceProposalStore proposalStore)
    {
        _proposalStore = proposalStore;
    }

    public (GovernanceProposalResult Result, GovernanceProposalCreatedEvent? Event) Execute(
        CreateGovernanceProposalCommand command)
    {
        if (string.IsNullOrWhiteSpace(command.ProposalTitle))
            return FailureCreate(command, "Proposal title must not be empty.");

        if (string.IsNullOrWhiteSpace(command.ProposalDescription))
            return FailureCreate(command, "Proposal description must not be empty.");

        if (!Enum.IsDefined(command.ProposalType))
            return FailureCreate(command, "Proposal type must be a valid enum value.");

        if (string.IsNullOrWhiteSpace(command.AuthorityDomain))
            return FailureCreate(command, "Authority domain must be defined.");

        if (command.ProposedByGuardianId == Guid.Empty)
            return FailureCreate(command, "Proposer guardian id must be valid.");

        if (command.ProposalId == Guid.Empty)
            return FailureCreate(command, "Proposal id must be valid.");

        var existing = _proposalStore.Get(command.ProposalId.ToString());
        if (existing is not null)
            return FailureCreate(command, "Proposal id already exists.");

        var result = new GovernanceProposalResult(
            Success: true,
            ProposalId: command.ProposalId,
            ProposalType: command.ProposalType,
            AuthorityDomain: command.AuthorityDomain,
            Action: GovernanceProposalAction.Created,
            Message: "Governance proposal created successfully.",
            ExecutedAt: command.Timestamp);

        var domainEvent = new GovernanceProposalCreatedEvent(
            EventId: Guid.NewGuid(),
            ProposalId: command.ProposalId,
            ProposalTitle: command.ProposalTitle,
            ProposalDescription: command.ProposalDescription,
            ProposalType: command.ProposalType.ToString(),
            AuthorityDomain: command.AuthorityDomain,
            ProposedByGuardianId: command.ProposedByGuardianId,
            CreatedAt: command.Timestamp,
            Metadata: command.Metadata);

        return (result, domainEvent);
    }

    public (GovernanceProposalResult Result, GovernanceProposalSubmittedEvent? Event) Execute(
        SubmitGovernanceProposalCommand command)
    {
        var proposal = _proposalStore.Get(command.ProposalId.ToString());
        if (proposal is null)
            return FailureSubmit(command.ProposalId, "Proposal not found in registry.");

        if (proposal.Status != ProposalStatus.Draft)
            return FailureSubmit(command.ProposalId,
                $"Proposal must be in Draft status to submit. Current status: {proposal.Status}");

        if (command.SubmittedByGuardianId == Guid.Empty)
            return FailureSubmit(command.ProposalId, "Submitter guardian id must be valid.");

        var result = new GovernanceProposalResult(
            Success: true,
            ProposalId: command.ProposalId,
            ProposalType: proposal.Type,
            AuthorityDomain: string.Empty,
            Action: GovernanceProposalAction.Submitted,
            Message: "Governance proposal submitted for review.",
            ExecutedAt: command.Timestamp);

        var domainEvent = new GovernanceProposalSubmittedEvent(
            EventId: Guid.NewGuid(),
            ProposalId: command.ProposalId,
            SubmittedByGuardianId: command.SubmittedByGuardianId,
            SubmittedAt: command.Timestamp);

        return (result, domainEvent);
    }

    public (GovernanceProposalResult Result, GovernanceProposalCancelledEvent? Event) Execute(
        CancelGovernanceProposalCommand command)
    {
        var proposal = _proposalStore.Get(command.ProposalId.ToString());
        if (proposal is null)
            return FailureCancel(command.ProposalId, "Proposal not found in registry.");

        if (proposal.Status == ProposalStatus.Cancelled)
            return FailureCancel(command.ProposalId, "Proposal is already cancelled.");

        if (proposal.Status is ProposalStatus.Approved or ProposalStatus.Closed)
            return FailureCancel(command.ProposalId,
                $"Cannot cancel a finalized proposal. Current status: {proposal.Status}");

        if (command.CancelledByGuardianId == Guid.Empty)
            return FailureCancel(command.ProposalId, "Canceller guardian id must be valid.");

        if (string.IsNullOrWhiteSpace(command.Reason))
            return FailureCancel(command.ProposalId, "Cancellation reason must be provided.");

        var result = new GovernanceProposalResult(
            Success: true,
            ProposalId: command.ProposalId,
            ProposalType: proposal.Type,
            AuthorityDomain: string.Empty,
            Action: GovernanceProposalAction.Cancelled,
            Message: "Governance proposal cancelled.",
            ExecutedAt: command.Timestamp);

        var domainEvent = new GovernanceProposalCancelledEvent(
            EventId: Guid.NewGuid(),
            ProposalId: command.ProposalId,
            CancelledByGuardianId: command.CancelledByGuardianId,
            Reason: command.Reason,
            CancelledAt: command.Timestamp);

        return (result, domainEvent);
    }

    // --- Existing store-based lifecycle methods ---

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

    private static (GovernanceProposalResult Result, GovernanceProposalCreatedEvent? Event) FailureCreate(
        CreateGovernanceProposalCommand command, string message)
    {
        return (new GovernanceProposalResult(false, command.ProposalId, command.ProposalType,
            command.AuthorityDomain, GovernanceProposalAction.Created, message, DateTime.UtcNow), null);
    }

    private static (GovernanceProposalResult Result, GovernanceProposalSubmittedEvent? Event) FailureSubmit(
        Guid proposalId, string message)
    {
        return (new GovernanceProposalResult(false, proposalId, default, string.Empty,
            GovernanceProposalAction.Submitted, message, DateTime.UtcNow), null);
    }

    private static (GovernanceProposalResult Result, GovernanceProposalCancelledEvent? Event) FailureCancel(
        Guid proposalId, string message)
    {
        return (new GovernanceProposalResult(false, proposalId, default, string.Empty,
            GovernanceProposalAction.Cancelled, message, DateTime.UtcNow), null);
    }
}
