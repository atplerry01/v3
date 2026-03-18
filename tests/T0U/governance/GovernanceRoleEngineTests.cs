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

public class GovernanceRoleEngineTests
{
    private readonly IdentityRegistry _identityRegistry = new();
    private readonly GuardianRegistryStore _guardianStore = new();
    private readonly GovernanceRoleStore _roleStore = new();
    private readonly GovernanceRoleEngine _engine;
    private readonly GuardianRegistryEngine _guardianEngine;

    private readonly Guid _guardianId;
    private readonly Guid _seniorGuardianId;

    public GovernanceRoleEngineTests()
    {
        _engine = new GovernanceRoleEngine(_roleStore, _guardianStore);
        _guardianEngine = new GuardianRegistryEngine(_guardianStore, _identityRegistry);

        // Register target guardian
        _guardianId = Guid.NewGuid();
        RegisterGuardian(_guardianId.ToString(), "Alice");

        // Register senior guardian (requester) with ConstitutionGuardian role
        _seniorGuardianId = Guid.NewGuid();
        RegisterGuardian(_seniorGuardianId.ToString(), "Bob");
        EnsureRoleInStore("ConstitutionGuardian");
        _roleStore.AssignRole(_seniorGuardianId.ToString(), "ConstitutionGuardian");
    }

    private void RegisterGuardian(string guardianId, string name)
    {
        var identityId = Guid.NewGuid();
        var identity = new IdentityAggregate(IdentityId.From(identityId), IdentityType.User);
        _identityRegistry.Register(identity);
        _guardianEngine.RegisterGuardian(guardianId, identityId, name, new List<string>());
    }

    private void EnsureRoleInStore(string roleId)
    {
        if (!_roleStore.RoleExists(roleId))
            _roleStore.AddRole(new GovernanceRole(roleId, roleId, $"System-defined {roleId} role", Array.Empty<string>()));
    }

    /// <summary>
    /// Simulates the runtime pipeline applying the assign event to the store.
    /// </summary>
    private void ApplyAssignToStore(Guid guardianId, GuardianRole role)
    {
        var roleId = role.ToString();
        EnsureRoleInStore(roleId);
        _roleStore.AssignRole(guardianId.ToString(), roleId);
    }

    /// <summary>
    /// Simulates the runtime pipeline applying the revoke event to the store.
    /// </summary>
    private void ApplyRevokeToStore(Guid guardianId, GuardianRole role)
    {
        _roleStore.RevokeRole(guardianId.ToString(), role.ToString());
    }

    // ──────────────────────────────────────────────
    // Command-based Execute tests — Assign
    // ──────────────────────────────────────────────

    [Fact]
    public void Execute_AssignRole_Succeeds()
    {
        var command = new AssignGovernanceRoleCommand(
            Guid.NewGuid(), _guardianId, GuardianRole.Guardian,
            "governance", _seniorGuardianId, DateTime.UtcNow);

        var (result, domainEvent) = _engine.Execute(command);

        Assert.True(result.Success);
        Assert.Equal(GuardianRole.Guardian, result.Role);
        Assert.Equal(GovernanceRoleAction.Assigned, result.Action);
        Assert.NotNull(domainEvent);
        Assert.Equal(_guardianId, domainEvent.GuardianId);
        Assert.Equal(GuardianRole.Guardian.ToString(), domainEvent.AssignedRole);
        Assert.Equal("governance", domainEvent.AuthorityDomain);
    }

    [Fact]
    public void Execute_AssignRole_InvalidGuardian_Fails()
    {
        var command = new AssignGovernanceRoleCommand(
            Guid.NewGuid(), Guid.NewGuid(), GuardianRole.Guardian,
            "governance", _seniorGuardianId, DateTime.UtcNow);

        var (result, domainEvent) = _engine.Execute(command);

        Assert.False(result.Success);
        Assert.Null(domainEvent);
        Assert.Contains("Guardian not found", result.Message);
    }

    [Fact]
    public void Execute_AssignRole_DuplicateRole_Fails()
    {
        var command = new AssignGovernanceRoleCommand(
            Guid.NewGuid(), _guardianId, GuardianRole.Guardian,
            "governance", _seniorGuardianId, DateTime.UtcNow);

        var (result1, _) = _engine.Execute(command);
        Assert.True(result1.Success);

        // Simulate runtime applying the event to the store
        ApplyAssignToStore(_guardianId, GuardianRole.Guardian);

        var (result2, domainEvent2) = _engine.Execute(command);

        Assert.False(result2.Success);
        Assert.Null(domainEvent2);
        Assert.Contains("already has role", result2.Message);
    }

    [Fact]
    public void Execute_AssignRole_EmptyAuthorityDomain_Fails()
    {
        var command = new AssignGovernanceRoleCommand(
            Guid.NewGuid(), _guardianId, GuardianRole.Guardian,
            "", _seniorGuardianId, DateTime.UtcNow);

        var (result, domainEvent) = _engine.Execute(command);

        Assert.False(result.Success);
        Assert.Null(domainEvent);
        Assert.Contains("Authority domain", result.Message);
    }

    [Fact]
    public void Execute_AssignRole_HierarchyViolation_Fails()
    {
        // Guardian with Guardian role tries to assign SeniorGuardian — should fail
        var lowGuardianId = Guid.NewGuid();
        RegisterGuardian(lowGuardianId.ToString(), "LowGuard");
        EnsureRoleInStore("Guardian");
        _roleStore.AssignRole(lowGuardianId.ToString(), "Guardian");

        var command = new AssignGovernanceRoleCommand(
            Guid.NewGuid(), _guardianId, GuardianRole.SeniorGuardian,
            "governance", lowGuardianId, DateTime.UtcNow);

        var (result, domainEvent) = _engine.Execute(command);

        Assert.False(result.Success);
        Assert.Null(domainEvent);
        Assert.Contains("sufficient authority", result.Message);
    }

    [Fact]
    public void Execute_AssignRole_HigherCanAssignLower()
    {
        // ConstitutionGuardian (level 4) assigns SeniorGuardian (level 3) — should succeed
        var command = new AssignGovernanceRoleCommand(
            Guid.NewGuid(), _guardianId, GuardianRole.SeniorGuardian,
            "governance", _seniorGuardianId, DateTime.UtcNow);

        var (result, domainEvent) = _engine.Execute(command);

        Assert.True(result.Success);
        Assert.NotNull(domainEvent);
    }

    [Fact]
    public void Execute_AssignRole_SameLevelCannotAssign()
    {
        // SeniorGuardian tries to assign SeniorGuardian — same level, should fail
        var peerGuardianId = Guid.NewGuid();
        RegisterGuardian(peerGuardianId.ToString(), "Peer");
        EnsureRoleInStore("SeniorGuardian");
        _roleStore.AssignRole(peerGuardianId.ToString(), "SeniorGuardian");

        var command = new AssignGovernanceRoleCommand(
            Guid.NewGuid(), _guardianId, GuardianRole.SeniorGuardian,
            "governance", peerGuardianId, DateTime.UtcNow);

        var (result, _) = _engine.Execute(command);

        Assert.False(result.Success);
        Assert.Contains("sufficient authority", result.Message);
    }

    // ──────────────────────────────────────────────
    // Command-based Execute tests — Revoke
    // ──────────────────────────────────────────────

    [Fact]
    public void Execute_RevokeRole_Succeeds()
    {
        // First assign
        var assignCmd = new AssignGovernanceRoleCommand(
            Guid.NewGuid(), _guardianId, GuardianRole.Guardian,
            "governance", _seniorGuardianId, DateTime.UtcNow);
        var (assignResult, _) = _engine.Execute(assignCmd);
        Assert.True(assignResult.Success);

        // Simulate runtime applying the assign event
        ApplyAssignToStore(_guardianId, GuardianRole.Guardian);

        // Then revoke
        var revokeCmd = new RevokeGovernanceRoleCommand(
            Guid.NewGuid(), _guardianId, GuardianRole.Guardian,
            "No longer needed", _seniorGuardianId, DateTime.UtcNow);
        var (result, domainEvent) = _engine.Execute(revokeCmd);

        Assert.True(result.Success);
        Assert.Equal(GovernanceRoleAction.Revoked, result.Action);
        Assert.NotNull(domainEvent);
        Assert.Equal(_guardianId, domainEvent.GuardianId);
        Assert.Equal(GuardianRole.Guardian.ToString(), domainEvent.RevokedRole);
        Assert.Equal("No longer needed", domainEvent.Reason);
    }

    [Fact]
    public void Execute_RevokeRole_NotAssigned_Fails()
    {
        var command = new RevokeGovernanceRoleCommand(
            Guid.NewGuid(), _guardianId, GuardianRole.Guardian,
            "Test", _seniorGuardianId, DateTime.UtcNow);

        var (result, domainEvent) = _engine.Execute(command);

        Assert.False(result.Success);
        Assert.Null(domainEvent);
        Assert.Contains("does not have role", result.Message);
    }

    [Fact]
    public void Execute_RevokeRole_HierarchyViolation_Fails()
    {
        // Assign a SeniorGuardian role first
        var assignCmd = new AssignGovernanceRoleCommand(
            Guid.NewGuid(), _guardianId, GuardianRole.SeniorGuardian,
            "governance", _seniorGuardianId, DateTime.UtcNow);
        _engine.Execute(assignCmd);
        ApplyAssignToStore(_guardianId, GuardianRole.SeniorGuardian);

        // Guardian-level requester tries to revoke SeniorGuardian — should fail
        var lowGuardianId = Guid.NewGuid();
        RegisterGuardian(lowGuardianId.ToString(), "LowGuard2");
        EnsureRoleInStore("Guardian");
        _roleStore.AssignRole(lowGuardianId.ToString(), "Guardian");

        var revokeCmd = new RevokeGovernanceRoleCommand(
            Guid.NewGuid(), _guardianId, GuardianRole.SeniorGuardian,
            "Test", lowGuardianId, DateTime.UtcNow);

        var (result, _) = _engine.Execute(revokeCmd);

        Assert.False(result.Success);
        Assert.Contains("sufficient authority", result.Message);
    }

    [Fact]
    public void Execute_RevokeRole_InvalidGuardian_Fails()
    {
        var command = new RevokeGovernanceRoleCommand(
            Guid.NewGuid(), Guid.NewGuid(), GuardianRole.Guardian,
            "Test", _seniorGuardianId, DateTime.UtcNow);

        var (result, domainEvent) = _engine.Execute(command);

        Assert.False(result.Success);
        Assert.Null(domainEvent);
        Assert.Contains("Guardian not found", result.Message);
    }

    // ──────────────────────────────────────────────
    // Hierarchy static method tests
    // ──────────────────────────────────────────────

    [Theory]
    [InlineData(GuardianRole.ConstitutionGuardian, GuardianRole.SeniorGuardian, true)]
    [InlineData(GuardianRole.ConstitutionGuardian, GuardianRole.Guardian, true)]
    [InlineData(GuardianRole.ConstitutionGuardian, GuardianRole.DomainGuardian, true)]
    [InlineData(GuardianRole.ConstitutionGuardian, GuardianRole.EmergencyGuardian, true)]
    [InlineData(GuardianRole.SeniorGuardian, GuardianRole.Guardian, true)]
    [InlineData(GuardianRole.SeniorGuardian, GuardianRole.SeniorGuardian, false)]
    [InlineData(GuardianRole.Guardian, GuardianRole.SeniorGuardian, false)]
    [InlineData(GuardianRole.Guardian, GuardianRole.ConstitutionGuardian, false)]
    [InlineData(GuardianRole.EmergencyGuardian, GuardianRole.Guardian, false)]
    [InlineData(GuardianRole.DomainGuardian, GuardianRole.Guardian, false)]
    public void CanAssignRole_RespectsHierarchy(GuardianRole requester, GuardianRole target, bool expected)
    {
        Assert.Equal(expected, GovernanceRoleEngine.CanAssignRole(requester, target));
    }

    // ──────────────────────────────────────────────
    // Concurrency tests
    // ──────────────────────────────────────────────

    [Fact]
    public void ConcurrentAssignments_ProduceDeterministicResults()
    {
        // Multiple concurrent assign commands for the same role should all succeed
        // since the engine does not write to the store — it only validates and produces events.
        // The runtime pipeline is responsible for applying events and enforcing uniqueness.
        var results = new List<GovernanceRoleResult>();
        var tasks = new List<Task>();

        for (int i = 0; i < 10; i++)
        {
            tasks.Add(Task.Run(() =>
            {
                var command = new AssignGovernanceRoleCommand(
                    Guid.NewGuid(), _guardianId, GuardianRole.DomainGuardian,
                    "governance", _seniorGuardianId, DateTime.UtcNow);

                var (result, _) = _engine.Execute(command);
                lock (results)
                {
                    results.Add(result);
                }
            }));
        }

        Task.WaitAll(tasks.ToArray());

        // All succeed because the engine is stateless — duplicate prevention
        // is handled by the runtime pipeline when applying events to the store.
        Assert.All(results, r => Assert.True(r.Success));
    }

    // ──────────────────────────────────────────────
    // Architecture validation tests
    // ──────────────────────────────────────────────

    [Fact]
    public void Engine_IsStateless_NoInstanceState()
    {
        var engine1 = new GovernanceRoleEngine(_roleStore, _guardianStore);
        var engine2 = new GovernanceRoleEngine(_roleStore, _guardianStore);

        var command = new AssignGovernanceRoleCommand(
            Guid.NewGuid(), _guardianId, GuardianRole.EmergencyGuardian,
            "governance", _seniorGuardianId, DateTime.UtcNow);

        var (result1, _) = engine1.Execute(command);
        Assert.True(result1.Success);

        // Both engines produce the same result for the same input state
        // (store has not been updated, so same command succeeds on engine2 too)
        var (result2, _) = engine2.Execute(command);
        Assert.True(result2.Success);
    }

    [Fact]
    public void Engine_DoesNotPersist()
    {
        var command = new AssignGovernanceRoleCommand(
            Guid.NewGuid(), _guardianId, GuardianRole.DomainGuardian,
            "governance", _seniorGuardianId, DateTime.UtcNow);

        var (result, _) = _engine.Execute(command);
        Assert.True(result.Success);

        // Engine did not write to store — guardian still has no DomainGuardian role in store
        var roleIds = _roleStore.GetGuardianRoleIds(_guardianId.ToString());
        Assert.DoesNotContain("DomainGuardian", roleIds);
    }

    [Fact]
    public void Engine_ProducesDeterministicResults()
    {
        var targetId = Guid.NewGuid();
        RegisterGuardian(targetId.ToString(), "Deterministic");

        var command = new AssignGovernanceRoleCommand(
            Guid.NewGuid(), targetId, GuardianRole.DomainGuardian,
            "governance", _seniorGuardianId, DateTime.UtcNow);

        var (result, _) = _engine.Execute(command);

        Assert.True(result.Success);
        Assert.Equal(targetId, result.GuardianId);
        Assert.Equal(GuardianRole.DomainGuardian, result.Role);
        Assert.Equal(GovernanceRoleAction.Assigned, result.Action);
    }
}
