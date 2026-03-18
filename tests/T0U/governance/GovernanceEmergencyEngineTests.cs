using Whycespace.Engines.T0U.Governance.Proposal.Validation;
using Whycespace.Engines.T0U.Governance.Proposal.Lifecycle;
using Whycespace.Engines.T0U.Governance.Voting.Casting;
using Whycespace.Engines.T0U.Governance.Quorum.Evaluation;
using Whycespace.Engines.T0U.Governance.Delegation.Assignment;
using Whycespace.Engines.T0U.Governance.Dispute.Raising;
using Whycespace.Engines.T0U.Governance.Emergency.Trigger;
using Whycespace.Engines.T0U.Governance.Roles.Assignment;
using Whycespace.Engines.T0U.Governance.Domain.Registration;
using Whycespace.Engines.T0U.Governance.ProposalType.Validation;
using Whycespace.Engines.T0U.Governance.Evidence.Recording;
using Whycespace.Engines.T0U.Governance.Evidence.Audit;
using Whycespace.Engines.T0U.Governance.Workflow.Execution;
using Whycespace.Engines.T0U.Governance.Decisions.Evaluation;
using Whycespace.Engines.T0U.Governance.Guardians.Registry;
using Whycespace.Engines.T0U.Governance.Proposal.Creation;
using Whycespace.Engines.T0U.Governance.Proposal.Submission;
using Whycespace.Engines.T0U.Governance.Proposal.Cancellation;
using Whycespace.Engines.T0U.Governance.Voting.Validation;
using Whycespace.Engines.T0U.Governance.Voting.Withdrawal;
using Whycespace.Engines.T0U.Governance.Delegation.Revocation;
using Whycespace.Engines.T0U.Governance.Dispute.Resolution;
using Whycespace.Engines.T0U.Governance.Dispute.Withdrawal;
using Whycespace.Engines.T0U.Governance.Emergency.Validation;
using Whycespace.Engines.T0U.Governance.Emergency.Revocation;
using Whycespace.Engines.T0U.Governance.Roles.Revocation;
using Whycespace.Engines.T0U.Governance.Domain.Validation;
using Whycespace.Engines.T0U.Governance.Domain.Deactivation;
using Whycespace.Engines.T0U.Governance.ProposalType.Registration;
using Whycespace.Engines.T0U.Governance.ProposalType.Deactivation;
using Whycespace.Systems.Upstream.Governance.Models;
using Whycespace.Systems.Upstream.Governance.Stores;
using Whycespace.Systems.WhyceID.Aggregates;
using Whycespace.Systems.WhyceID.Models;
using Whycespace.Systems.WhyceID.Registry;

namespace Whycespace.Governance.Tests;

public class GovernanceEmergencyEngineTests
{
    private readonly GovernanceEmergencyStore _emergencyStore = new();
    private readonly GuardianRegistryStore _guardianStore = new();
    private readonly GovernanceEmergencyEngine _engine;

    public GovernanceEmergencyEngineTests()
    {
        _engine = new GovernanceEmergencyEngine(_emergencyStore, _guardianStore);

        var identityRegistry = new IdentityRegistry();
        var identityId = Guid.NewGuid();
        identityRegistry.Register(new IdentityAggregate(IdentityId.From(identityId), IdentityType.User));
        var guardianEngine = new GuardianRegistryEngine(_guardianStore, identityRegistry);
        guardianEngine.RegisterGuardian("g-alice", identityId, "Alice", new List<string>());
        guardianEngine.ActivateGuardian("g-alice");

        guardianEngine.RegisterGuardian("g-inactive", identityId, "Inactive", new List<string>());
    }

    // --- Trigger Tests ---

    [Fact]
    public void Execute_TriggerEmergency_SystemPause_Succeeds()
    {
        var command = new TriggerEmergencyActionCommand(
            Guid.NewGuid(), "e-1", EmergencyType.SystemPause, "economic", "g-alice",
            "Critical vulnerability detected", DateTime.UtcNow);

        var result = _engine.Execute(command);

        Assert.True(result.Success);
        Assert.Equal("e-1", result.EmergencyActionId);
        Assert.Equal(EmergencyType.SystemPause, result.EmergencyType);
        Assert.Equal(EmergencyStatus.Active, result.EmergencyStatus);
        Assert.Equal("economic", result.TargetDomain);
    }

    [Fact]
    public void Execute_TriggerEmergency_SecurityLockdown_Succeeds()
    {
        var command = new TriggerEmergencyActionCommand(
            Guid.NewGuid(), "e-2", EmergencyType.SecurityLockdown, "identity", "g-alice",
            "Breach detected", DateTime.UtcNow);

        var result = _engine.Execute(command);

        Assert.True(result.Success);
        Assert.Equal(EmergencyType.SecurityLockdown, result.EmergencyType);
    }

    [Fact]
    public void Execute_TriggerEmergency_ClusterFreeze_Succeeds()
    {
        var command = new TriggerEmergencyActionCommand(
            Guid.NewGuid(), "e-3", EmergencyType.ClusterFreeze, "governance", "g-alice",
            "Cluster integrity issue", DateTime.UtcNow);

        var result = _engine.Execute(command);

        Assert.True(result.Success);
        Assert.Equal(EmergencyType.ClusterFreeze, result.EmergencyType);
    }

    [Fact]
    public void Execute_TriggerEmergency_PolicyOverride_Succeeds()
    {
        var command = new TriggerEmergencyActionCommand(
            Guid.NewGuid(), "e-4", EmergencyType.PolicyOverride, "policy", "g-alice",
            "Urgent policy change", DateTime.UtcNow);

        var result = _engine.Execute(command);

        Assert.True(result.Success);
        Assert.Equal(EmergencyType.PolicyOverride, result.EmergencyType);
    }

    [Fact]
    public void Execute_TriggerEmergency_ExecutionHalt_Succeeds()
    {
        var command = new TriggerEmergencyActionCommand(
            Guid.NewGuid(), "e-5", EmergencyType.ExecutionHalt, "runtime", "g-alice",
            "Runtime failure", DateTime.UtcNow);

        var result = _engine.Execute(command);

        Assert.True(result.Success);
        Assert.Equal(EmergencyType.ExecutionHalt, result.EmergencyType);
    }

    [Fact]
    public void Execute_TriggerEmergency_EmergencyVoteOverride_Succeeds()
    {
        var command = new TriggerEmergencyActionCommand(
            Guid.NewGuid(), "e-6", EmergencyType.EmergencyVoteOverride, "governance", "g-alice",
            "Vote override required", DateTime.UtcNow);

        var result = _engine.Execute(command);

        Assert.True(result.Success);
        Assert.Equal(EmergencyType.EmergencyVoteOverride, result.EmergencyType);
    }

    [Fact]
    public void Execute_TriggerEmergency_InvalidGuardian_Fails()
    {
        var command = new TriggerEmergencyActionCommand(
            Guid.NewGuid(), "e-bad", EmergencyType.SystemPause, "economic", "nonexistent",
            "Reason", DateTime.UtcNow);

        var result = _engine.Execute(command);

        Assert.False(result.Success);
        Assert.Contains("Guardian not found", result.Message);
    }

    [Fact]
    public void Execute_TriggerEmergency_InactiveGuardian_Fails()
    {
        var command = new TriggerEmergencyActionCommand(
            Guid.NewGuid(), "e-bad", EmergencyType.SystemPause, "economic", "g-inactive",
            "Reason", DateTime.UtcNow);

        var result = _engine.Execute(command);

        Assert.False(result.Success);
        Assert.Contains("Only active guardians", result.Message);
    }

    [Fact]
    public void Execute_TriggerEmergency_EmptyReason_Fails()
    {
        var command = new TriggerEmergencyActionCommand(
            Guid.NewGuid(), "e-bad", EmergencyType.SystemPause, "economic", "g-alice",
            "", DateTime.UtcNow);

        var result = _engine.Execute(command);

        Assert.False(result.Success);
        Assert.Contains("reason is required", result.Message);
    }

    [Fact]
    public void Execute_TriggerEmergency_EmptyTargetDomain_Fails()
    {
        var command = new TriggerEmergencyActionCommand(
            Guid.NewGuid(), "e-bad", EmergencyType.SystemPause, "", "g-alice",
            "Reason", DateTime.UtcNow);

        var result = _engine.Execute(command);

        Assert.False(result.Success);
        Assert.Contains("Target domain is required", result.Message);
    }

    [Fact]
    public void Execute_TriggerEmergency_DuplicateId_Fails()
    {
        var command = new TriggerEmergencyActionCommand(
            Guid.NewGuid(), "e-dup", EmergencyType.SystemPause, "economic", "g-alice",
            "Reason", DateTime.UtcNow);

        _engine.Execute(command);
        var result = _engine.Execute(command with { CommandId = Guid.NewGuid() });

        Assert.False(result.Success);
        Assert.Contains("already exists", result.Message);
    }

    [Fact]
    public void Execute_TriggerEmergency_StoresEmergencyCorrectly()
    {
        var timestamp = DateTime.UtcNow;
        var command = new TriggerEmergencyActionCommand(
            Guid.NewGuid(), "e-store", EmergencyType.SecurityLockdown, "identity", "g-alice",
            "Store test", timestamp);

        _engine.Execute(command);

        var stored = _engine.GetEmergency("e-store");
        Assert.NotNull(stored);
        Assert.Equal(EmergencyType.SecurityLockdown, stored.Type);
        Assert.Equal("identity", stored.TargetDomain);
        Assert.Equal("g-alice", stored.TriggeredBy);
        Assert.Equal("Store test", stored.Reason);
        Assert.Equal(EmergencyStatus.Active, stored.Status);
        Assert.Equal(timestamp, stored.TriggeredAt);
        Assert.Null(stored.ResolvedAt);
    }

    // --- Revoke Tests ---

    [Fact]
    public void Execute_RevokeEmergency_Succeeds()
    {
        var trigger = new TriggerEmergencyActionCommand(
            Guid.NewGuid(), "e-rev", EmergencyType.SystemPause, "economic", "g-alice",
            "Reason", DateTime.UtcNow);
        _engine.Execute(trigger);

        var revoke = new RevokeEmergencyActionCommand(
            Guid.NewGuid(), "e-rev", "g-alice", "Issue resolved", DateTime.UtcNow);
        var result = _engine.Execute(revoke);

        Assert.True(result.Success);
        Assert.Equal(EmergencyStatus.Revoked, result.EmergencyStatus);
        Assert.Contains("revoked successfully", result.Message);
    }

    [Fact]
    public void Execute_RevokeEmergency_NotFound_Fails()
    {
        var revoke = new RevokeEmergencyActionCommand(
            Guid.NewGuid(), "nonexistent", "g-alice", "Reason", DateTime.UtcNow);
        var result = _engine.Execute(revoke);

        Assert.False(result.Success);
        Assert.Contains("Emergency not found", result.Message);
    }

    [Fact]
    public void Execute_RevokeEmergency_AlreadyRevoked_Fails()
    {
        var trigger = new TriggerEmergencyActionCommand(
            Guid.NewGuid(), "e-rr", EmergencyType.SystemPause, "economic", "g-alice",
            "Reason", DateTime.UtcNow);
        _engine.Execute(trigger);

        var revoke = new RevokeEmergencyActionCommand(
            Guid.NewGuid(), "e-rr", "g-alice", "First revoke", DateTime.UtcNow);
        _engine.Execute(revoke);

        var result = _engine.Execute(revoke with { CommandId = Guid.NewGuid(), Reason = "Second revoke" });

        Assert.False(result.Success);
        Assert.Contains("cannot be revoked", result.Message);
    }

    [Fact]
    public void Execute_RevokeEmergency_EmptyReason_Fails()
    {
        var trigger = new TriggerEmergencyActionCommand(
            Guid.NewGuid(), "e-nr", EmergencyType.SystemPause, "economic", "g-alice",
            "Reason", DateTime.UtcNow);
        _engine.Execute(trigger);

        var revoke = new RevokeEmergencyActionCommand(
            Guid.NewGuid(), "e-nr", "g-alice", "", DateTime.UtcNow);
        var result = _engine.Execute(revoke);

        Assert.False(result.Success);
        Assert.Contains("Revocation reason is required", result.Message);
    }

    [Fact]
    public void Execute_RevokeEmergency_InvalidGuardian_Fails()
    {
        var trigger = new TriggerEmergencyActionCommand(
            Guid.NewGuid(), "e-rg", EmergencyType.SystemPause, "economic", "g-alice",
            "Reason", DateTime.UtcNow);
        _engine.Execute(trigger);

        var revoke = new RevokeEmergencyActionCommand(
            Guid.NewGuid(), "e-rg", "nonexistent", "Reason", DateTime.UtcNow);
        var result = _engine.Execute(revoke);

        Assert.False(result.Success);
        Assert.Contains("Guardian not found", result.Message);
    }

    [Fact]
    public void Execute_RevokeEmergency_InactiveGuardian_Fails()
    {
        var trigger = new TriggerEmergencyActionCommand(
            Guid.NewGuid(), "e-ri", EmergencyType.SystemPause, "economic", "g-alice",
            "Reason", DateTime.UtcNow);
        _engine.Execute(trigger);

        var revoke = new RevokeEmergencyActionCommand(
            Guid.NewGuid(), "e-ri", "g-inactive", "Reason", DateTime.UtcNow);
        var result = _engine.Execute(revoke);

        Assert.False(result.Success);
        Assert.Contains("Only active guardians can revoke", result.Message);
    }

    [Fact]
    public void Execute_RevokeEmergency_SetsResolvedAt()
    {
        var trigger = new TriggerEmergencyActionCommand(
            Guid.NewGuid(), "e-ts", EmergencyType.SystemPause, "economic", "g-alice",
            "Reason", DateTime.UtcNow);
        _engine.Execute(trigger);

        var revokeTimestamp = DateTime.UtcNow;
        var revoke = new RevokeEmergencyActionCommand(
            Guid.NewGuid(), "e-ts", "g-alice", "Resolved now", revokeTimestamp);
        _engine.Execute(revoke);

        var stored = _engine.GetEmergency("e-ts");
        Assert.NotNull(stored);
        Assert.Equal(EmergencyStatus.Revoked, stored.Status);
        Assert.Equal(revokeTimestamp, stored.ResolvedAt);
    }

    // --- Validate Tests ---

    [Fact]
    public void Execute_ValidateEmergency_Succeeds()
    {
        var trigger = new TriggerEmergencyActionCommand(
            Guid.NewGuid(), "e-val", EmergencyType.SecurityLockdown, "identity", "g-alice",
            "Reason", DateTime.UtcNow);
        _engine.Execute(trigger);

        var validate = new ValidateEmergencyActionCommand(
            Guid.NewGuid(), "e-val", "g-alice", DateTime.UtcNow);
        var result = _engine.Execute(validate);

        Assert.True(result.Success);
        Assert.Equal(EmergencyStatus.Active, result.EmergencyStatus);
        Assert.Contains("validated successfully", result.Message);
    }

    [Fact]
    public void Execute_ValidateEmergency_NotFound_Fails()
    {
        var validate = new ValidateEmergencyActionCommand(
            Guid.NewGuid(), "nonexistent", "g-alice", DateTime.UtcNow);
        var result = _engine.Execute(validate);

        Assert.False(result.Success);
        Assert.Contains("Emergency not found", result.Message);
    }

    [Fact]
    public void Execute_ValidateEmergency_InvalidGuardian_Fails()
    {
        var trigger = new TriggerEmergencyActionCommand(
            Guid.NewGuid(), "e-vg", EmergencyType.SystemPause, "economic", "g-alice",
            "Reason", DateTime.UtcNow);
        _engine.Execute(trigger);

        var validate = new ValidateEmergencyActionCommand(
            Guid.NewGuid(), "e-vg", "nonexistent", DateTime.UtcNow);
        var result = _engine.Execute(validate);

        Assert.False(result.Success);
        Assert.Contains("Guardian not found", result.Message);
    }

    [Fact]
    public void Execute_ValidateEmergency_InactiveGuardian_Fails()
    {
        var trigger = new TriggerEmergencyActionCommand(
            Guid.NewGuid(), "e-vi", EmergencyType.SystemPause, "economic", "g-alice",
            "Reason", DateTime.UtcNow);
        _engine.Execute(trigger);

        var validate = new ValidateEmergencyActionCommand(
            Guid.NewGuid(), "e-vi", "g-inactive", DateTime.UtcNow);
        var result = _engine.Execute(validate);

        Assert.False(result.Success);
        Assert.Contains("Only active guardians can validate", result.Message);
    }

    // --- Concurrency Tests ---

    [Fact]
    public void Execute_ConcurrentTriggers_AllSucceed()
    {
        var tasks = Enumerable.Range(0, 10).Select(i =>
        {
            var command = new TriggerEmergencyActionCommand(
                Guid.NewGuid(), $"e-conc-{i}", EmergencyType.SystemPause, "economic", "g-alice",
                $"Concurrent trigger {i}", DateTime.UtcNow);
            return Task.Run(() => _engine.Execute(command));
        }).ToArray();

        Task.WaitAll(tasks);

        Assert.All(tasks, t => Assert.True(t.Result.Success));
        Assert.Equal(10, _engine.ListEmergencies().Count);
    }

    [Fact]
    public void Execute_DeterministicExecution_SameInputSameOutput()
    {
        var timestamp = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var command = new TriggerEmergencyActionCommand(
            Guid.NewGuid(), "e-det-1", EmergencyType.ClusterFreeze, "governance", "g-alice",
            "Deterministic test", timestamp);

        var result1 = _engine.Execute(command);

        var command2 = command with { EmergencyActionId = "e-det-2", CommandId = Guid.NewGuid() };
        var result2 = _engine.Execute(command2);

        Assert.Equal(result1.Success, result2.Success);
        Assert.Equal(result1.EmergencyType, result2.EmergencyType);
        Assert.Equal(result1.EmergencyStatus, result2.EmergencyStatus);
        Assert.Equal(result1.TargetDomain, result2.TargetDomain);
    }

    // --- Query Tests ---

    [Fact]
    public void GetEmergency_ReturnsNull_WhenNotFound()
    {
        Assert.Null(_engine.GetEmergency("nonexistent"));
    }

    [Fact]
    public void ListEmergencies_ReturnsAll()
    {
        _engine.Execute(new TriggerEmergencyActionCommand(
            Guid.NewGuid(), "e-list-1", EmergencyType.SystemPause, "economic", "g-alice",
            "Reason 1", DateTime.UtcNow));
        _engine.Execute(new TriggerEmergencyActionCommand(
            Guid.NewGuid(), "e-list-2", EmergencyType.SecurityLockdown, "identity", "g-alice",
            "Reason 2", DateTime.UtcNow));

        var emergencies = _engine.ListEmergencies();

        Assert.Equal(2, emergencies.Count);
    }

    // --- Architecture Tests ---

    [Fact]
    public void Engine_IsSealed()
    {
        Assert.True(typeof(GovernanceEmergencyEngine).IsSealed);
    }

    [Fact]
    public void Engine_HasNoStaticMutableState()
    {
        var fields = typeof(GovernanceEmergencyEngine)
            .GetFields(global::System.Reflection.BindingFlags.Static | global::System.Reflection.BindingFlags.NonPublic | global::System.Reflection.BindingFlags.Public);

        Assert.All(fields, f => Assert.True(f.IsInitOnly || f.IsLiteral,
            $"Static field {f.Name} must be readonly or const."));
    }
}
