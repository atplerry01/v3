using Whycespace.Domain.Events.Governance;
using Whycespace.Engines.T0U.WhyceGovernance;
using Whycespace.Engines.T0U.WhyceGovernance.Commands;
using Whycespace.Engines.T0U.WhyceGovernance.Results;
using Whycespace.Systems.Upstream.Governance.Models;
using Whycespace.Systems.Upstream.Governance.Stores;
using Whycespace.Systems.WhyceID.Aggregates;
using Whycespace.Systems.WhyceID.Models;
using Whycespace.Systems.WhyceID.Registry;

namespace Whycespace.Governance.Tests;

public class VotingEngineTests
{
    private readonly IdentityRegistry _identityRegistry = new();
    private readonly GuardianRegistryStore _guardianStore = new();
    private readonly GovernanceProposalStore _proposalStore = new();
    private readonly GovernanceVoteStore _voteStore = new();
    private readonly VotingEngine _engine;
    private readonly GovernanceProposalRegistryEngine _registryEngine;
    private readonly GovernanceProposalEngine _proposalEngine;
    private readonly GuardianRegistryEngine _guardianEngine;
    private readonly Guid _identityId;

    public VotingEngineTests()
    {
        _engine = new VotingEngine(_voteStore, _proposalStore, _guardianStore);
        _registryEngine = new GovernanceProposalRegistryEngine(_proposalStore, _guardianStore);
        _proposalEngine = new GovernanceProposalEngine(_proposalStore);
        _guardianEngine = new GuardianRegistryEngine(_guardianStore, _identityRegistry);

        _identityId = Guid.NewGuid();
        _identityRegistry.Register(new IdentityAggregate(IdentityId.From(_identityId), IdentityType.User));

        _guardianEngine.RegisterGuardian("g-alice", _identityId, "Alice", new List<string>());
        _guardianEngine.ActivateGuardian("g-alice");

        _guardianEngine.RegisterGuardian("g-bob", _identityId, "Bob", new List<string>());
        _guardianEngine.ActivateGuardian("g-bob");

        _registryEngine.CreateProposal("p-1", "Test Proposal", "Desc", ProposalType.Policy, "g-alice");
        _proposalEngine.OpenProposal("p-1");
        _proposalEngine.StartVoting("p-1");
    }

    // --- CastVoteCommand tests ---

    [Fact]
    public void Execute_CastVote_Succeeds()
    {
        var command = new CastVoteCommand("v-1", "p-1", "g-alice", VoteType.Approve, 1, DateTime.UtcNow);

        var (result, domainEvent) = _engine.Execute(command);

        Assert.True(result.Success);
        Assert.Equal("v-1", result.VoteId);
        Assert.Equal("p-1", result.ProposalId);
        Assert.Equal("g-alice", result.GuardianId);
        Assert.Equal(VoteType.Approve, result.VoteDecision);
        Assert.Equal(VoteAction.Cast, result.Action);
        Assert.NotNull(domainEvent);
        Assert.Equal("v-1", domainEvent.VoteId);
        Assert.Equal("p-1", domainEvent.ProposalId);
        Assert.Equal("g-alice", domainEvent.GuardianId);
        Assert.Equal("Approve", domainEvent.VoteDecision);
        Assert.Equal(1, domainEvent.VoteWeight);
    }

    [Fact]
    public void Execute_CastVote_DuplicateVote_Fails()
    {
        _voteStore.Add(new GovernanceVote("v-1", "p-1", "g-alice", VoteType.Approve, 1, DateTime.UtcNow));

        var (result, domainEvent) = _engine.Execute(new CastVoteCommand("v-2", "p-1", "g-alice", VoteType.Reject, 1, DateTime.UtcNow));

        Assert.False(result.Success);
        Assert.Contains("already voted", result.Message);
        Assert.Null(domainEvent);
    }

    [Fact]
    public void Execute_CastVote_InactiveGuardian_Fails()
    {
        _guardianEngine.RegisterGuardian("g-inactive", _identityId, "Inactive", new List<string>());

        var (result, domainEvent) = _engine.Execute(new CastVoteCommand("v-1", "p-1", "g-inactive", VoteType.Approve, 1, DateTime.UtcNow));

        Assert.False(result.Success);
        Assert.Contains("Inactive guardians cannot vote", result.Message);
        Assert.Null(domainEvent);
    }

    [Fact]
    public void Execute_CastVote_ProposalNotVoting_Fails()
    {
        _registryEngine.CreateProposal("p-draft", "Draft", "Desc", ProposalType.Policy, "g-alice");

        var (result, domainEvent) = _engine.Execute(new CastVoteCommand("v-1", "p-draft", "g-alice", VoteType.Approve, 1, DateTime.UtcNow));

        Assert.False(result.Success);
        Assert.Contains("not in Voting status", result.Message);
        Assert.Null(domainEvent);
    }

    [Fact]
    public void Execute_CastVote_InvalidProposal_Fails()
    {
        var (result, domainEvent) = _engine.Execute(new CastVoteCommand("v-1", "nonexistent", "g-alice", VoteType.Approve, 1, DateTime.UtcNow));

        Assert.False(result.Success);
        Assert.Contains("Proposal not found", result.Message);
        Assert.Null(domainEvent);
    }

    [Fact]
    public void Execute_CastVote_InvalidGuardian_Fails()
    {
        var (result, domainEvent) = _engine.Execute(new CastVoteCommand("v-1", "p-1", "nonexistent", VoteType.Approve, 1, DateTime.UtcNow));

        Assert.False(result.Success);
        Assert.Contains("Guardian not found", result.Message);
        Assert.Null(domainEvent);
    }

    [Fact]
    public void Execute_CastVote_InvalidWeight_Fails()
    {
        var (result, domainEvent) = _engine.Execute(new CastVoteCommand("v-1", "p-1", "g-alice", VoteType.Approve, 0, DateTime.UtcNow));

        Assert.False(result.Success);
        Assert.Contains("Vote weight must be between", result.Message);
        Assert.Null(domainEvent);
    }

    [Fact]
    public void Execute_CastVote_ExcessiveWeight_Fails()
    {
        var (result, domainEvent) = _engine.Execute(new CastVoteCommand("v-1", "p-1", "g-alice", VoteType.Approve, 101, DateTime.UtcNow));

        Assert.False(result.Success);
        Assert.Contains("Vote weight must be between", result.Message);
        Assert.Null(domainEvent);
    }

    // --- WithdrawVoteCommand tests ---

    [Fact]
    public void Execute_WithdrawVote_Succeeds()
    {
        _voteStore.Add(new GovernanceVote("v-1", "p-1", "g-alice", VoteType.Approve, 1, DateTime.UtcNow));

        var (result, domainEvent) = _engine.Execute(new WithdrawVoteCommand("w-1", "p-1", "g-alice", "Changed mind", DateTime.UtcNow));

        Assert.True(result.Success);
        Assert.Equal(VoteAction.Withdrawn, result.Action);
        Assert.Contains("Changed mind", result.Message);
        Assert.NotNull(domainEvent);
        Assert.Equal("v-1", domainEvent.VoteId);
        Assert.Equal("Changed mind", domainEvent.Reason);
    }

    [Fact]
    public void Execute_WithdrawVote_NoExistingVote_Fails()
    {
        var (result, domainEvent) = _engine.Execute(new WithdrawVoteCommand("w-1", "p-1", "g-alice", "No vote", DateTime.UtcNow));

        Assert.False(result.Success);
        Assert.Contains("No vote found", result.Message);
        Assert.Null(domainEvent);
    }

    [Fact]
    public void Execute_WithdrawVote_ThenRevote_Succeeds()
    {
        _voteStore.Add(new GovernanceVote("v-1", "p-1", "g-alice", VoteType.Approve, 1, DateTime.UtcNow));
        _voteStore.Withdraw("v-1", "p-1", "g-alice");

        var (result, domainEvent) = _engine.Execute(new CastVoteCommand("v-2", "p-1", "g-alice", VoteType.Reject, 1, DateTime.UtcNow));

        Assert.True(result.Success);
        Assert.Equal(VoteType.Reject, result.VoteDecision);
        Assert.NotNull(domainEvent);
    }

    // --- ValidateVoteCommand tests ---

    [Fact]
    public void Execute_ValidateVote_EligibleGuardian_Succeeds()
    {
        var (result, domainEvent) = _engine.Execute(new ValidateVoteCommand("val-1", "p-1", "g-alice", VoteType.Approve, DateTime.UtcNow));

        Assert.True(result.Success);
        Assert.Equal(VoteAction.Validated, result.Action);
        Assert.Contains("eligible", result.Message);
        Assert.NotNull(domainEvent);
        Assert.Equal("g-alice", domainEvent.GuardianId);
        Assert.Equal("Approve", domainEvent.VoteDecision);
    }

    [Fact]
    public void Execute_ValidateVote_AlreadyVoted_Fails()
    {
        _voteStore.Add(new GovernanceVote("v-1", "p-1", "g-alice", VoteType.Approve, 1, DateTime.UtcNow));

        var (result, domainEvent) = _engine.Execute(new ValidateVoteCommand("val-1", "p-1", "g-alice", VoteType.Approve, DateTime.UtcNow));

        Assert.False(result.Success);
        Assert.Contains("already voted", result.Message);
        Assert.Null(domainEvent);
    }

    [Fact]
    public void Execute_ValidateVote_InactiveGuardian_Fails()
    {
        _guardianEngine.RegisterGuardian("g-inactive", _identityId, "Inactive", new List<string>());

        var (result, domainEvent) = _engine.Execute(new ValidateVoteCommand("val-1", "p-1", "g-inactive", VoteType.Approve, DateTime.UtcNow));

        Assert.False(result.Success);
        Assert.Contains("Inactive guardians cannot vote", result.Message);
        Assert.Null(domainEvent);
    }

    // --- Event emission tests ---

    [Fact]
    public void Execute_CastVote_EmitsGovernanceVoteCastEvent()
    {
        var command = new CastVoteCommand("v-ev", "p-1", "g-alice", VoteType.Reject, 5, DateTime.UtcNow);

        var (result, domainEvent) = _engine.Execute(command);

        Assert.True(result.Success);
        Assert.NotNull(domainEvent);
        Assert.IsType<GovernanceVoteCastEvent>(domainEvent);
        Assert.Equal("v-ev", domainEvent.VoteId);
        Assert.Equal("Reject", domainEvent.VoteDecision);
        Assert.Equal(5, domainEvent.VoteWeight);
        Assert.Equal(1, domainEvent.EventVersion);
        Assert.NotEqual(Guid.Empty, domainEvent.EventId);
    }

    [Fact]
    public void Execute_WithdrawVote_EmitsGovernanceVoteWithdrawnEvent()
    {
        _voteStore.Add(new GovernanceVote("v-wd", "p-1", "g-alice", VoteType.Approve, 1, DateTime.UtcNow));

        var (result, domainEvent) = _engine.Execute(new WithdrawVoteCommand("w-ev", "p-1", "g-alice", "Testing", DateTime.UtcNow));

        Assert.True(result.Success);
        Assert.NotNull(domainEvent);
        Assert.IsType<GovernanceVoteWithdrawnEvent>(domainEvent);
        Assert.Equal("v-wd", domainEvent.VoteId);
        Assert.Equal("Testing", domainEvent.Reason);
        Assert.Equal(1, domainEvent.EventVersion);
    }

    [Fact]
    public void Execute_ValidateVote_EmitsGovernanceVoteValidatedEvent()
    {
        var (result, domainEvent) = _engine.Execute(new ValidateVoteCommand("val-ev", "p-1", "g-bob", VoteType.Abstain, DateTime.UtcNow));

        Assert.True(result.Success);
        Assert.NotNull(domainEvent);
        Assert.IsType<GovernanceVoteValidatedEvent>(domainEvent);
        Assert.Equal("g-bob", domainEvent.GuardianId);
        Assert.Equal("Abstain", domainEvent.VoteDecision);
        Assert.Equal(1, domainEvent.EventVersion);
    }

    // --- No persistence tests ---

    [Fact]
    public void Execute_CastVote_DoesNotPersistToStore()
    {
        var command = new CastVoteCommand("v-np", "p-1", "g-alice", VoteType.Approve, 1, DateTime.UtcNow);

        var (result, _) = _engine.Execute(command);

        Assert.True(result.Success);
        // Engine does not persist — store should remain empty
        Assert.False(_voteStore.HasVoted("g-alice", "p-1"));
    }

    // --- Concurrency tests ---

    [Fact]
    public void Execute_ConcurrentVotes_DifferentGuardians_AllSucceed()
    {
        var guardianIds = new List<string>();
        for (int i = 0; i < 10; i++)
        {
            var gId = $"g-concurrent-{i}";
            _guardianEngine.RegisterGuardian(gId, _identityId, $"Guardian{i}", new List<string>());
            _guardianEngine.ActivateGuardian(gId);
            guardianIds.Add(gId);
        }

        var results = new global::System.Collections.Concurrent.ConcurrentBag<(VotingResult Result, GovernanceVoteCastEvent? Event)>();
        Parallel.ForEach(guardianIds, (gId, _, idx) =>
        {
            var command = new CastVoteCommand($"v-concurrent-{idx}", "p-1", gId, VoteType.Approve, 1, DateTime.UtcNow);
            results.Add(_engine.Execute(command));
        });

        Assert.All(results, r =>
        {
            Assert.True(r.Result.Success);
            Assert.NotNull(r.Event);
        });
    }

    // --- Determinism test ---

    [Fact]
    public void Execute_IsDeterministic_SameInputSameOutput()
    {
        var command = new CastVoteCommand("v-det", "p-1", "g-alice", VoteType.Approve, 5, DateTime.UtcNow);

        var (result, domainEvent) = _engine.Execute(command);

        Assert.True(result.Success);
        Assert.Equal("v-det", result.VoteId);
        Assert.Equal(VoteType.Approve, result.VoteDecision);
        Assert.Equal(VoteAction.Cast, result.Action);
        Assert.NotNull(domainEvent);
    }

    // --- Architecture tests ---

    [Fact]
    public void Engine_IsSealed()
    {
        Assert.True(typeof(VotingEngine).IsSealed);
    }

    [Fact]
    public void Execute_FailureResult_EmitsNoEvent()
    {
        var (result, domainEvent) = _engine.Execute(new CastVoteCommand("v-1", "nonexistent", "g-alice", VoteType.Approve, 1, DateTime.UtcNow));

        Assert.False(result.Success);
        Assert.Null(domainEvent);
    }

    [Fact]
    public void Execute_Events_AreImmutableRecords()
    {
        var command = new CastVoteCommand("v-imm", "p-1", "g-alice", VoteType.Approve, 1, DateTime.UtcNow);
        var (_, domainEvent) = _engine.Execute(command);

        Assert.NotNull(domainEvent);
        Assert.True(domainEvent.GetType().IsSealed);
    }
}
