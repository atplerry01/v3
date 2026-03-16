using Whycespace.Engines.T0U.Governance;
using Whycespace.Engines.T0U.Governance.Commands;
using Whycespace.Engines.T0U.Governance.Results;
using Whycespace.Systems.Upstream.Governance.Models;
using Whycespace.Systems.Upstream.Governance.Stores;
using Whycespace.Systems.WhyceID.Aggregates;
using Whycespace.Systems.WhyceID.Models;
using Whycespace.Systems.WhyceID.Registry;

namespace Whycespace.Governance.Tests;

public class GovernanceProposalEngineTests
{
    private readonly GovernanceProposalStore _proposalStore = new();
    private readonly GuardianRegistryStore _guardianStore = new();
    private readonly GovernanceProposalEngine _engine;
    private readonly GovernanceProposalRegistryEngine _registryEngine;

    public GovernanceProposalEngineTests()
    {
        _engine = new GovernanceProposalEngine(_proposalStore);
        _registryEngine = new GovernanceProposalRegistryEngine(_proposalStore, _guardianStore);

        var identityRegistry = new IdentityRegistry();
        var identityId = Guid.NewGuid();
        identityRegistry.Register(new IdentityAggregate(IdentityId.From(identityId), IdentityType.User));
        var guardianEngine = new GuardianRegistryEngine(_guardianStore, identityRegistry);
        guardianEngine.RegisterGuardian("g-alice", identityId, "Alice", new List<string>());
    }

    private GovernanceProposal CreateDraftProposal(string id = "p-1")
    {
        return _registryEngine.CreateProposal(id, "Test Proposal", "Description", ProposalType.Policy, "g-alice");
    }

    // --- Existing store-based lifecycle tests ---

    [Fact]
    public void OpenProposal_FromDraft_Succeeds()
    {
        CreateDraftProposal();

        var result = _engine.OpenProposal("p-1");

        Assert.Equal(ProposalStatus.Open, result.Status);
    }

    [Fact]
    public void OpenProposal_NotDraft_Throws()
    {
        CreateDraftProposal();
        _engine.OpenProposal("p-1");

        var ex = Assert.Throws<InvalidOperationException>(() =>
            _engine.OpenProposal("p-1"));
        Assert.Contains("must be in Draft", ex.Message);
    }

    [Fact]
    public void StartVoting_FromOpen_Succeeds()
    {
        CreateDraftProposal();
        _engine.OpenProposal("p-1");

        var result = _engine.StartVoting("p-1");

        Assert.Equal(ProposalStatus.Voting, result.Status);
    }

    [Fact]
    public void StartVoting_NotOpen_Throws()
    {
        CreateDraftProposal();

        var ex = Assert.Throws<InvalidOperationException>(() =>
            _engine.StartVoting("p-1"));
        Assert.Contains("must be Open", ex.Message);
    }

    [Fact]
    public void RejectProposal_FromVoting_Succeeds()
    {
        CreateDraftProposal();
        _engine.OpenProposal("p-1");
        _engine.StartVoting("p-1");

        var result = _engine.RejectProposal("p-1");

        Assert.Equal(ProposalStatus.Rejected, result.Status);
    }

    [Fact]
    public void RejectProposal_NotVoting_Throws()
    {
        CreateDraftProposal();
        _engine.OpenProposal("p-1");

        var ex = Assert.Throws<InvalidOperationException>(() =>
            _engine.RejectProposal("p-1"));
        Assert.Contains("must be in Voting", ex.Message);
    }

    [Fact]
    public void CloseProposal_FromOpen_Succeeds()
    {
        CreateDraftProposal();
        _engine.OpenProposal("p-1");

        var result = _engine.CloseProposal("p-1");

        Assert.Equal(ProposalStatus.Closed, result.Status);
    }

    [Fact]
    public void CloseProposal_FromVoting_Succeeds()
    {
        CreateDraftProposal();
        _engine.OpenProposal("p-1");
        _engine.StartVoting("p-1");

        var result = _engine.CloseProposal("p-1");

        Assert.Equal(ProposalStatus.Closed, result.Status);
    }

    [Fact]
    public void CloseProposal_AlreadyClosed_Throws()
    {
        CreateDraftProposal();
        _engine.OpenProposal("p-1");
        _engine.CloseProposal("p-1");

        var ex = Assert.Throws<InvalidOperationException>(() =>
            _engine.CloseProposal("p-1"));
        Assert.Contains("already closed", ex.Message);
    }

    [Fact]
    public void CloseProposal_FromDraft_Throws()
    {
        CreateDraftProposal();

        var ex = Assert.Throws<InvalidOperationException>(() =>
            _engine.CloseProposal("p-1"));
        Assert.Contains("Cannot close a Draft", ex.Message);
    }

    [Fact]
    public void NotFound_Throws()
    {
        var ex = Assert.Throws<KeyNotFoundException>(() =>
            _engine.OpenProposal("nonexistent"));
        Assert.Contains("Proposal not found", ex.Message);
    }

    // --- Command-based Execute tests ---

    private static CreateGovernanceProposalCommand ValidCreateCommand(Guid? proposalId = null) =>
        new(
            CommandId: Guid.NewGuid(),
            ProposalId: proposalId ?? Guid.NewGuid(),
            ProposalTitle: "Constitutional Amendment",
            ProposalDescription: "Amend voting threshold from 60% to 75%",
            ProposalType: ProposalType.Constitutional,
            AuthorityDomain: "governance.core",
            ProposedByGuardianId: Guid.NewGuid(),
            Metadata: new Dictionary<string, string> { ["priority"] = "high" },
            Timestamp: DateTime.UtcNow);

    [Fact]
    public void Execute_CreateProposal_Succeeds()
    {
        var command = ValidCreateCommand();

        var (result, domainEvent) = _engine.Execute(command);

        Assert.True(result.Success);
        Assert.Equal(command.ProposalId, result.ProposalId);
        Assert.Equal(GovernanceProposalAction.Created, result.Action);
        Assert.Equal(ProposalType.Constitutional, result.ProposalType);
        Assert.Equal("governance.core", result.AuthorityDomain);
        Assert.NotNull(domainEvent);
        Assert.Equal(command.ProposalId, domainEvent.ProposalId);
        Assert.Equal("Constitutional", domainEvent.ProposalType);
        Assert.Equal("Constitutional Amendment", domainEvent.ProposalTitle);
    }

    [Fact]
    public void Execute_CreateProposal_EmptyTitle_Fails()
    {
        var command = ValidCreateCommand() with { ProposalTitle = "" };

        var (result, domainEvent) = _engine.Execute(command);

        Assert.False(result.Success);
        Assert.Contains("title must not be empty", result.Message);
        Assert.Null(domainEvent);
    }

    [Fact]
    public void Execute_CreateProposal_EmptyDescription_Fails()
    {
        var command = ValidCreateCommand() with { ProposalDescription = "" };

        var (result, domainEvent) = _engine.Execute(command);

        Assert.False(result.Success);
        Assert.Contains("description must not be empty", result.Message);
        Assert.Null(domainEvent);
    }

    [Fact]
    public void Execute_CreateProposal_InvalidType_Fails()
    {
        var command = ValidCreateCommand() with { ProposalType = (ProposalType)999 };

        var (result, domainEvent) = _engine.Execute(command);

        Assert.False(result.Success);
        Assert.Contains("valid enum", result.Message);
        Assert.Null(domainEvent);
    }

    [Fact]
    public void Execute_CreateProposal_EmptyAuthorityDomain_Fails()
    {
        var command = ValidCreateCommand() with { AuthorityDomain = "" };

        var (result, domainEvent) = _engine.Execute(command);

        Assert.False(result.Success);
        Assert.Contains("Authority domain", result.Message);
        Assert.Null(domainEvent);
    }

    [Fact]
    public void Execute_CreateProposal_EmptyGuardianId_Fails()
    {
        var command = ValidCreateCommand() with { ProposedByGuardianId = Guid.Empty };

        var (result, domainEvent) = _engine.Execute(command);

        Assert.False(result.Success);
        Assert.Contains("guardian id must be valid", result.Message);
        Assert.Null(domainEvent);
    }

    [Fact]
    public void Execute_CreateProposal_EmptyProposalId_Fails()
    {
        var command = ValidCreateCommand() with { ProposalId = Guid.Empty };

        var (result, domainEvent) = _engine.Execute(command);

        Assert.False(result.Success);
        Assert.Contains("Proposal id must be valid", result.Message);
        Assert.Null(domainEvent);
    }

    [Fact]
    public void Execute_CreateProposal_DuplicateId_Fails()
    {
        var proposalId = Guid.NewGuid();
        CreateDraftProposal(proposalId.ToString());

        var command = ValidCreateCommand(proposalId);

        var (result, domainEvent) = _engine.Execute(command);

        Assert.False(result.Success);
        Assert.Contains("already exists", result.Message);
        Assert.Null(domainEvent);
    }

    [Fact]
    public void Execute_SubmitProposal_Succeeds()
    {
        var proposalId = Guid.NewGuid();
        CreateDraftProposal(proposalId.ToString());

        var command = new SubmitGovernanceProposalCommand(
            CommandId: Guid.NewGuid(),
            ProposalId: proposalId,
            SubmittedByGuardianId: Guid.NewGuid(),
            Timestamp: DateTime.UtcNow);

        var (result, domainEvent) = _engine.Execute(command);

        Assert.True(result.Success);
        Assert.Equal(GovernanceProposalAction.Submitted, result.Action);
        Assert.NotNull(domainEvent);
        Assert.Equal(proposalId, domainEvent.ProposalId);
    }

    [Fact]
    public void Execute_SubmitProposal_NotFound_Fails()
    {
        var command = new SubmitGovernanceProposalCommand(
            CommandId: Guid.NewGuid(),
            ProposalId: Guid.NewGuid(),
            SubmittedByGuardianId: Guid.NewGuid(),
            Timestamp: DateTime.UtcNow);

        var (result, domainEvent) = _engine.Execute(command);

        Assert.False(result.Success);
        Assert.Contains("not found", result.Message);
        Assert.Null(domainEvent);
    }

    [Fact]
    public void Execute_SubmitProposal_NotDraft_Fails()
    {
        var proposalId = Guid.NewGuid();
        CreateDraftProposal(proposalId.ToString());
        _engine.OpenProposal(proposalId.ToString());

        var command = new SubmitGovernanceProposalCommand(
            CommandId: Guid.NewGuid(),
            ProposalId: proposalId,
            SubmittedByGuardianId: Guid.NewGuid(),
            Timestamp: DateTime.UtcNow);

        var (result, domainEvent) = _engine.Execute(command);

        Assert.False(result.Success);
        Assert.Contains("Draft status", result.Message);
        Assert.Null(domainEvent);
    }

    [Fact]
    public void Execute_CancelProposal_FromDraft_Succeeds()
    {
        var proposalId = Guid.NewGuid();
        CreateDraftProposal(proposalId.ToString());

        var command = new CancelGovernanceProposalCommand(
            CommandId: Guid.NewGuid(),
            ProposalId: proposalId,
            CancelledByGuardianId: Guid.NewGuid(),
            Reason: "No longer needed",
            Timestamp: DateTime.UtcNow);

        var (result, domainEvent) = _engine.Execute(command);

        Assert.True(result.Success);
        Assert.Equal(GovernanceProposalAction.Cancelled, result.Action);
        Assert.NotNull(domainEvent);
        Assert.Equal("No longer needed", domainEvent.Reason);
    }

    [Fact]
    public void Execute_CancelProposal_NotFound_Fails()
    {
        var command = new CancelGovernanceProposalCommand(
            CommandId: Guid.NewGuid(),
            ProposalId: Guid.NewGuid(),
            CancelledByGuardianId: Guid.NewGuid(),
            Reason: "Withdrawn",
            Timestamp: DateTime.UtcNow);

        var (result, domainEvent) = _engine.Execute(command);

        Assert.False(result.Success);
        Assert.Contains("not found", result.Message);
        Assert.Null(domainEvent);
    }

    [Fact]
    public void Execute_CancelProposal_Finalized_Fails()
    {
        var proposalId = Guid.NewGuid();
        CreateDraftProposal(proposalId.ToString());
        _engine.OpenProposal(proposalId.ToString());
        _engine.CloseProposal(proposalId.ToString());

        var command = new CancelGovernanceProposalCommand(
            CommandId: Guid.NewGuid(),
            ProposalId: proposalId,
            CancelledByGuardianId: Guid.NewGuid(),
            Reason: "Changed mind",
            Timestamp: DateTime.UtcNow);

        var (result, domainEvent) = _engine.Execute(command);

        Assert.False(result.Success);
        Assert.Contains("finalized", result.Message);
        Assert.Null(domainEvent);
    }

    [Fact]
    public void Execute_CancelProposal_EmptyReason_Fails()
    {
        var proposalId = Guid.NewGuid();
        CreateDraftProposal(proposalId.ToString());

        var command = new CancelGovernanceProposalCommand(
            CommandId: Guid.NewGuid(),
            ProposalId: proposalId,
            CancelledByGuardianId: Guid.NewGuid(),
            Reason: "",
            Timestamp: DateTime.UtcNow);

        var (result, domainEvent) = _engine.Execute(command);

        Assert.False(result.Success);
        Assert.Contains("reason must be provided", result.Message);
        Assert.Null(domainEvent);
    }

    [Fact]
    public void Execute_CancelProposal_AlreadyCancelled_Fails()
    {
        var proposalId = Guid.NewGuid();
        CreateDraftProposal(proposalId.ToString());

        // First cancel succeeds
        var cancelCommand = new CancelGovernanceProposalCommand(
            CommandId: Guid.NewGuid(),
            ProposalId: proposalId,
            CancelledByGuardianId: Guid.NewGuid(),
            Reason: "First cancel",
            Timestamp: DateTime.UtcNow);
        var (firstResult, _) = _engine.Execute(cancelCommand);
        Assert.True(firstResult.Success);

        // Update store to reflect cancelled status
        var proposal = _proposalStore.Get(proposalId.ToString())!;
        _proposalStore.Update(proposal with { Status = ProposalStatus.Cancelled });

        // Second cancel should fail
        var secondCommand = new CancelGovernanceProposalCommand(
            CommandId: Guid.NewGuid(),
            ProposalId: proposalId,
            CancelledByGuardianId: Guid.NewGuid(),
            Reason: "Second cancel",
            Timestamp: DateTime.UtcNow);
        var (result, domainEvent) = _engine.Execute(secondCommand);

        Assert.False(result.Success);
        Assert.Contains("already cancelled", result.Message);
        Assert.Null(domainEvent);
    }

    [Fact]
    public void Execute_SubmitProposal_EmptyGuardianId_Fails()
    {
        var proposalId = Guid.NewGuid();
        CreateDraftProposal(proposalId.ToString());

        var command = new SubmitGovernanceProposalCommand(
            CommandId: Guid.NewGuid(),
            ProposalId: proposalId,
            SubmittedByGuardianId: Guid.Empty,
            Timestamp: DateTime.UtcNow);

        var (result, domainEvent) = _engine.Execute(command);

        Assert.False(result.Success);
        Assert.Contains("guardian id must be valid", result.Message);
        Assert.Null(domainEvent);
    }

    [Fact]
    public void Execute_CancelProposal_EmptyGuardianId_Fails()
    {
        var proposalId = Guid.NewGuid();
        CreateDraftProposal(proposalId.ToString());

        var command = new CancelGovernanceProposalCommand(
            CommandId: Guid.NewGuid(),
            ProposalId: proposalId,
            CancelledByGuardianId: Guid.Empty,
            Reason: "Some reason",
            Timestamp: DateTime.UtcNow);

        var (result, domainEvent) = _engine.Execute(command);

        Assert.False(result.Success);
        Assert.Contains("guardian id must be valid", result.Message);
        Assert.Null(domainEvent);
    }

    [Fact]
    public void Execute_CancelProposal_Approved_Fails()
    {
        var proposalId = Guid.NewGuid();
        CreateDraftProposal(proposalId.ToString());
        _engine.OpenProposal(proposalId.ToString());
        _engine.StartVoting(proposalId.ToString());

        // Manually update store to Approved status
        var proposal = _proposalStore.Get(proposalId.ToString())!;
        _proposalStore.Update(proposal with { Status = ProposalStatus.Approved });

        var command = new CancelGovernanceProposalCommand(
            CommandId: Guid.NewGuid(),
            ProposalId: proposalId,
            CancelledByGuardianId: Guid.NewGuid(),
            Reason: "Too late",
            Timestamp: DateTime.UtcNow);

        var (result, domainEvent) = _engine.Execute(command);

        Assert.False(result.Success);
        Assert.Contains("finalized", result.Message);
        Assert.Null(domainEvent);
    }

    // --- Architecture verification tests ---

    [Fact]
    public void Engine_IsSealed()
    {
        Assert.True(typeof(GovernanceProposalEngine).IsSealed);
    }

    [Fact]
    public void Engine_ExecuteMethods_DoNotPersist()
    {
        var store = new GovernanceProposalStore();
        var engine = new GovernanceProposalEngine(store);

        var command = ValidCreateCommand();
        var (result, _) = engine.Execute(command);

        Assert.True(result.Success);
        // Engine should NOT have added to the store — persistence is handled by the runtime pipeline
        Assert.Null(store.Get(command.ProposalId.ToString()));
    }

    [Fact]
    public void Engine_HasNoEngineToEngineDependencies()
    {
        var engineType = typeof(GovernanceProposalEngine);
        var constructorParams = engineType.GetConstructors()
            .SelectMany(c => c.GetParameters())
            .Select(p => p.ParameterType);

        // No constructor parameter should be another engine type
        Assert.DoesNotContain(constructorParams,
            p => p.Namespace?.Contains("Engines") == true && p != engineType);
    }

    [Fact]
    public void Execute_ConcurrentCreations_AllSucceed()
    {
        var tasks = Enumerable.Range(0, 10)
            .Select(_ => ValidCreateCommand())
            .Select(cmd => Task.Run(() => _engine.Execute(cmd)))
            .ToArray();

        Task.WaitAll(tasks);

        Assert.All(tasks, t => Assert.True(t.Result.Result.Success));
    }

    [Fact]
    public void Execute_CreateProposal_IsDeterministic()
    {
        var command = ValidCreateCommand();

        var (result1, _) = _engine.Execute(command);
        var store2 = new GovernanceProposalStore();
        var engine2 = new GovernanceProposalEngine(store2);
        var (result2, _) = engine2.Execute(command);

        Assert.Equal(result1.Success, result2.Success);
        Assert.Equal(result1.ProposalId, result2.ProposalId);
        Assert.Equal(result1.Action, result2.Action);
    }
}
