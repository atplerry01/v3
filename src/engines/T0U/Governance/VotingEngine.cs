namespace Whycespace.Engines.T0U.Governance;

using Whycespace.Domain.Events.Governance;
using Whycespace.Engines.T0U.Governance.Commands;
using Whycespace.Engines.T0U.Governance.Results;
using Whycespace.Systems.Upstream.Governance.Models;
using Whycespace.Systems.Upstream.Governance.Stores;

public sealed class VotingEngine
{
    private const int MinVoteWeight = 1;
    private const int MaxVoteWeight = 100;

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

    public (VotingResult Result, GovernanceVoteCastEvent? Event) Execute(CastVoteCommand command)
    {
        var proposal = _proposalStore.Get(command.ProposalId);
        if (proposal is null)
            return (VotingResult.Fail(command.ProposalId, command.GuardianId, VoteAction.Cast, $"Proposal not found: {command.ProposalId}"), null);

        if (proposal.Status != ProposalStatus.Voting)
            return (VotingResult.Fail(command.ProposalId, command.GuardianId, VoteAction.Cast, $"Proposal is not in Voting status. Current status: {proposal.Status}"), null);

        var guardian = _guardianStore.GetGuardian(command.GuardianId);
        if (guardian is null)
            return (VotingResult.Fail(command.ProposalId, command.GuardianId, VoteAction.Cast, $"Guardian not found: {command.GuardianId}"), null);

        if (guardian.Status != GuardianStatus.Active)
            return (VotingResult.Fail(command.ProposalId, command.GuardianId, VoteAction.Cast, $"Inactive guardians cannot vote. Guardian status: {guardian.Status}"), null);

        if (command.VoteWeight < MinVoteWeight || command.VoteWeight > MaxVoteWeight)
            return (VotingResult.Fail(command.ProposalId, command.GuardianId, VoteAction.Cast, $"Vote weight must be between {MinVoteWeight} and {MaxVoteWeight}. Provided: {command.VoteWeight}"), null);

        if (_voteStore.HasVoted(command.GuardianId, command.ProposalId))
            return (VotingResult.Fail(command.ProposalId, command.GuardianId, VoteAction.Cast, $"Guardian '{command.GuardianId}' has already voted on proposal '{command.ProposalId}'."), null);

        var voteId = command.CommandId;

        var result = VotingResult.Ok(voteId, command.ProposalId, command.GuardianId, command.VoteDecision, VoteAction.Cast, "Vote cast successfully.");

        var domainEvent = GovernanceVoteCastEvent.Create(
            voteId, command.ProposalId, command.GuardianId,
            command.VoteDecision.ToString(), command.VoteWeight);

        return (result, domainEvent);
    }

    public (VotingResult Result, GovernanceVoteWithdrawnEvent? Event) Execute(WithdrawVoteCommand command)
    {
        var proposal = _proposalStore.Get(command.ProposalId);
        if (proposal is null)
            return (VotingResult.Fail(command.ProposalId, command.GuardianId, VoteAction.Withdrawn, $"Proposal not found: {command.ProposalId}"), null);

        if (proposal.Status != ProposalStatus.Voting)
            return (VotingResult.Fail(command.ProposalId, command.GuardianId, VoteAction.Withdrawn, $"Proposal is not in Voting status. Current status: {proposal.Status}"), null);

        var existingVote = _voteStore.GetByGuardianAndProposal(command.GuardianId, command.ProposalId);
        if (existingVote is null)
            return (VotingResult.Fail(command.ProposalId, command.GuardianId, VoteAction.Withdrawn, $"No vote found for guardian '{command.GuardianId}' on proposal '{command.ProposalId}'."), null);

        var result = VotingResult.Ok(existingVote.VoteId, command.ProposalId, command.GuardianId, existingVote.Vote, VoteAction.Withdrawn, $"Vote withdrawn. Reason: {command.Reason}");

        var domainEvent = GovernanceVoteWithdrawnEvent.Create(
            existingVote.VoteId, command.ProposalId, command.GuardianId, command.Reason);

        return (result, domainEvent);
    }

    public (VotingResult Result, GovernanceVoteValidatedEvent? Event) Execute(ValidateVoteCommand command)
    {
        var proposal = _proposalStore.Get(command.ProposalId);
        if (proposal is null)
            return (VotingResult.Fail(command.ProposalId, command.GuardianId, VoteAction.Validated, $"Proposal not found: {command.ProposalId}"), null);

        if (proposal.Status != ProposalStatus.Voting)
            return (VotingResult.Fail(command.ProposalId, command.GuardianId, VoteAction.Validated, $"Proposal is not in Voting status. Current status: {proposal.Status}"), null);

        var guardian = _guardianStore.GetGuardian(command.GuardianId);
        if (guardian is null)
            return (VotingResult.Fail(command.ProposalId, command.GuardianId, VoteAction.Validated, $"Guardian not found: {command.GuardianId}"), null);

        if (guardian.Status != GuardianStatus.Active)
            return (VotingResult.Fail(command.ProposalId, command.GuardianId, VoteAction.Validated, $"Inactive guardians cannot vote. Guardian status: {guardian.Status}"), null);

        if (_voteStore.HasVoted(command.GuardianId, command.ProposalId))
            return (VotingResult.Fail(command.ProposalId, command.GuardianId, VoteAction.Validated, $"Guardian '{command.GuardianId}' has already voted on proposal '{command.ProposalId}'."), null);

        var result = VotingResult.Ok(string.Empty, command.ProposalId, command.GuardianId, command.VoteDecision, VoteAction.Validated, "Vote validated. Guardian is eligible to vote.");

        var domainEvent = GovernanceVoteValidatedEvent.Create(
            command.ProposalId, command.GuardianId, command.VoteDecision.ToString());

        return (result, domainEvent);
    }
}
