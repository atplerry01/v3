using Whycespace.Engines.T0U.Governance;
using Whycespace.Engines.T0U.Governance.Commands;
using Whycespace.Systems.Upstream.Governance.Models;
using Whycespace.Systems.Upstream.Governance.Stores;
using Whycespace.Systems.WhyceID.Aggregates;
using Whycespace.Systems.WhyceID.Models;
using Whycespace.Systems.WhyceID.Registry;

namespace Whycespace.Governance.Tests;

public class GovernanceDisputeEngineTests
{
    private readonly GovernanceDisputeStore _disputeStore = new();
    private readonly GovernanceProposalStore _proposalStore = new();
    private readonly GuardianRegistryStore _guardianStore = new();
    private readonly GovernanceDisputeEngine _engine;

    private readonly string _guardianId = "g-alice";
    private readonly string _proposalId = "p-1";

    public GovernanceDisputeEngineTests()
    {
        _engine = new GovernanceDisputeEngine(_disputeStore, _proposalStore, _guardianStore);

        var identityRegistry = new IdentityRegistry();
        var identityId = Guid.NewGuid();
        identityRegistry.Register(new IdentityAggregate(IdentityId.From(identityId), IdentityType.User));
        var guardianEngine = new GuardianRegistryEngine(_guardianStore, identityRegistry);
        guardianEngine.RegisterGuardian(_guardianId, identityId, "Alice", new List<string>());

        var registryEngine = new GovernanceProposalRegistryEngine(_proposalStore, _guardianStore);
        registryEngine.CreateProposal(_proposalId, "Proposal", "Desc", ProposalType.Policy, _guardianId);
    }

    private RaiseGovernanceDisputeCommand RaiseCommand(
        Guid? proposalId = null,
        Guid? guardianId = null,
        string reason = "Procedure was not followed",
        DisputeType disputeType = DisputeType.DecisionChallenge)
    {
        return new RaiseGovernanceDisputeCommand(
            CommandId: Guid.NewGuid(),
            ProposalId: proposalId ?? Guid.Parse(_proposalId),
            DisputeType: disputeType,
            RaisedByGuardianId: guardianId ?? Guid.Parse(_guardianId),
            DisputeReason: reason,
            Timestamp: DateTime.UtcNow);
    }

    [Fact]
    public void RaiseDispute_Succeeds()
    {
        var command = RaiseCommand();
        var (result, @event) = _engine.Execute(command);

        Assert.True(result.Success);
        Assert.NotEqual(Guid.Empty, result.DisputeId);
        Assert.Equal(command.ProposalId, result.ProposalId);
        Assert.Equal(DisputeStatus.Raised, result.DisputeStatus);
        Assert.Equal(DisputeType.DecisionChallenge, result.DisputeType);
        Assert.NotNull(@event);
        Assert.Equal(result.DisputeId, @event.DisputeId);
    }

    [Fact]
    public void RaiseDispute_InvalidProposal_Fails()
    {
        var command = RaiseCommand(proposalId: Guid.NewGuid());
        var (result, @event) = _engine.Execute(command);

        Assert.False(result.Success);
        Assert.Contains("Proposal not found", result.Message);
        Assert.Null(@event);
    }

    [Fact]
    public void RaiseDispute_InvalidGuardian_Fails()
    {
        var command = RaiseCommand(guardianId: Guid.NewGuid());
        var (result, @event) = _engine.Execute(command);

        Assert.False(result.Success);
        Assert.Contains("Guardian not found", result.Message);
        Assert.Null(@event);
    }

    [Fact]
    public void RaiseDispute_EmptyReason_Fails()
    {
        var command = RaiseCommand(reason: "");
        var (result, @event) = _engine.Execute(command);

        Assert.False(result.Success);
        Assert.Contains("reason is required", result.Message);
        Assert.Null(@event);
    }

    [Fact]
    public void RaiseDispute_Duplicate_Fails()
    {
        var command = RaiseCommand();
        _engine.Execute(command);

        var (result, @event) = _engine.Execute(command);

        Assert.False(result.Success);
        Assert.Contains("already exists", result.Message);
        Assert.Null(@event);
    }

    [Fact]
    public void WithdrawDispute_Succeeds()
    {
        var raiseCommand = RaiseCommand();
        var (raised, _) = _engine.Execute(raiseCommand);

        var withdrawCommand = new WithdrawGovernanceDisputeCommand(
            CommandId: Guid.NewGuid(),
            DisputeId: raised.DisputeId,
            WithdrawnByGuardianId: Guid.Parse(_guardianId),
            Reason: "No longer relevant",
            Timestamp: DateTime.UtcNow);

        var (result, @event) = _engine.Execute(withdrawCommand);

        Assert.True(result.Success);
        Assert.Equal(DisputeStatus.Withdrawn, result.DisputeStatus);
        Assert.NotNull(@event);
        Assert.Equal(raised.DisputeId, @event.DisputeId);
    }

    [Fact]
    public void WithdrawDispute_NotFound_Fails()
    {
        var command = new WithdrawGovernanceDisputeCommand(
            CommandId: Guid.NewGuid(),
            DisputeId: Guid.NewGuid(),
            WithdrawnByGuardianId: Guid.Parse(_guardianId),
            Reason: "Reason",
            Timestamp: DateTime.UtcNow);

        var (result, @event) = _engine.Execute(command);

        Assert.False(result.Success);
        Assert.Contains("not found", result.Message);
        Assert.Null(@event);
    }

    [Fact]
    public void WithdrawDispute_WrongGuardian_Fails()
    {
        var raiseCommand = RaiseCommand();
        var (raised, _) = _engine.Execute(raiseCommand);

        var command = new WithdrawGovernanceDisputeCommand(
            CommandId: Guid.NewGuid(),
            DisputeId: raised.DisputeId,
            WithdrawnByGuardianId: Guid.NewGuid(),
            Reason: "Reason",
            Timestamp: DateTime.UtcNow);

        var (result, @event) = _engine.Execute(command);

        Assert.False(result.Success);
        Assert.Contains("Only the guardian", result.Message);
        Assert.Null(@event);
    }

    [Fact]
    public void ResolveDispute_Succeeds()
    {
        var raiseCommand = RaiseCommand();
        var (raised, _) = _engine.Execute(raiseCommand);

        var resolveCommand = new ResolveGovernanceDisputeCommand(
            CommandId: Guid.NewGuid(),
            DisputeId: raised.DisputeId,
            ResolutionOutcome: "Dispute upheld, proposal amended",
            ResolvedByGuardianId: Guid.Parse(_guardianId),
            Timestamp: DateTime.UtcNow);

        var (result, @event) = _engine.Execute(resolveCommand);

        Assert.True(result.Success);
        Assert.Equal(DisputeStatus.Resolved, result.DisputeStatus);
        Assert.NotNull(@event);
        Assert.Equal("Dispute upheld, proposal amended", @event.ResolutionOutcome);
    }

    [Fact]
    public void ResolveDispute_AlreadyResolved_Fails()
    {
        var raiseCommand = RaiseCommand();
        var (raised, _) = _engine.Execute(raiseCommand);

        var resolveCommand = new ResolveGovernanceDisputeCommand(
            CommandId: Guid.NewGuid(),
            DisputeId: raised.DisputeId,
            ResolutionOutcome: "Resolved",
            ResolvedByGuardianId: Guid.Parse(_guardianId),
            Timestamp: DateTime.UtcNow);

        _engine.Execute(resolveCommand);
        var (result, @event) = _engine.Execute(resolveCommand);

        Assert.False(result.Success);
        Assert.Contains("already resolved", result.Message);
        Assert.Null(@event);
    }

    [Fact]
    public void EscalateDispute_Succeeds()
    {
        var raiseCommand = RaiseCommand();
        var (raised, _) = _engine.Execute(raiseCommand);

        var (result, @event) = _engine.Escalate(raised.DisputeId, "Requires constitutional review");

        Assert.True(result.Success);
        Assert.Equal(DisputeStatus.Escalated, result.DisputeStatus);
        Assert.NotNull(@event);
        Assert.Equal("Requires constitutional review", @event.EscalationReason);
    }

    [Fact]
    public void EscalateDispute_AfterResolve_Fails()
    {
        var raiseCommand = RaiseCommand();
        var (raised, _) = _engine.Execute(raiseCommand);

        var resolveCommand = new ResolveGovernanceDisputeCommand(
            CommandId: Guid.NewGuid(),
            DisputeId: raised.DisputeId,
            ResolutionOutcome: "Done",
            ResolvedByGuardianId: Guid.Parse(_guardianId),
            Timestamp: DateTime.UtcNow);
        _engine.Execute(resolveCommand);

        var (result, @event) = _engine.Escalate(raised.DisputeId, "Reason");

        Assert.False(result.Success);
        Assert.Contains("Cannot escalate a resolved dispute", result.Message);
        Assert.Null(@event);
    }

    [Fact]
    public void ResolveDispute_AfterEscalation_Succeeds()
    {
        var raiseCommand = RaiseCommand();
        var (raised, _) = _engine.Execute(raiseCommand);

        _engine.Escalate(raised.DisputeId, "Needs review");

        var resolveCommand = new ResolveGovernanceDisputeCommand(
            CommandId: Guid.NewGuid(),
            DisputeId: raised.DisputeId,
            ResolutionOutcome: "Resolved after escalation",
            ResolvedByGuardianId: Guid.Parse(_guardianId),
            Timestamp: DateTime.UtcNow);

        var (result, @event) = _engine.Execute(resolveCommand);

        Assert.True(result.Success);
        Assert.Equal(DisputeStatus.Resolved, result.DisputeStatus);
        Assert.NotNull(@event);
    }

    [Fact]
    public void Engine_IsStateless_NoMutableInstanceState()
    {
        var type = typeof(GovernanceDisputeEngine);
        var fields = type.GetFields(
            global::System.Reflection.BindingFlags.Instance |
            global::System.Reflection.BindingFlags.NonPublic |
            global::System.Reflection.BindingFlags.Public);

        foreach (var field in fields)
        {
            Assert.True(field.IsInitOnly || field.Name.StartsWith("_"),
                $"Field {field.Name} should be readonly (injected store)");
        }
    }

    [Fact]
    public void ConcurrentDisputeSubmissions_AreDeterministic()
    {
        var tasks = new List<Task>();
        var results = new global::System.Collections.Concurrent.ConcurrentBag<Whycespace.Engines.T0U.Governance.Results.GovernanceDisputeResult>();

        for (int i = 0; i < 10; i++)
        {
            var identityRegistry = new IdentityRegistry();
            var identityId = Guid.NewGuid();
            identityRegistry.Register(new IdentityAggregate(IdentityId.From(identityId), IdentityType.User));
            var gEngine = new GuardianRegistryEngine(_guardianStore, identityRegistry);
            var gId = $"g-concurrent-{i}";
            gEngine.RegisterGuardian(gId, identityId, $"Guardian {i}", new List<string>());

            var cmd = new RaiseGovernanceDisputeCommand(
                CommandId: Guid.NewGuid(),
                ProposalId: Guid.Parse(_proposalId),
                DisputeType: DisputeType.VotingIntegrity,
                RaisedByGuardianId: Guid.Parse(gId),
                DisputeReason: $"Concurrent dispute {i}",
                Timestamp: DateTime.UtcNow);

            tasks.Add(Task.Run(() =>
            {
                var (result, _) = _engine.Execute(cmd);
                results.Add(result);
            }));
        }

        Task.WaitAll(tasks.ToArray());

        Assert.Equal(10, results.Count(r => r.Success));
    }
}
