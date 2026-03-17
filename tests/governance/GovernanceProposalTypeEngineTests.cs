using Whycespace.Engines.T0U.WhyceGovernance.Engines;
using Whycespace.Engines.T0U.WhyceGovernance.Commands;
using Whycespace.Engines.T0U.WhyceGovernance.Results;
using Whycespace.Systems.Upstream.Governance.Models;
using Whycespace.Systems.Upstream.Governance.Stores;
using Whycespace.Systems.WhyceID.Aggregates;
using Whycespace.Systems.WhyceID.Models;
using Whycespace.Systems.WhyceID.Registry;

namespace Whycespace.Governance.Tests;

public class GovernanceProposalTypeEngineTests
{
    // --- Registration Tests ---

    [Fact]
    public void Execute_RegisterType_Succeeds()
    {
        var engine = CreateEngineWithGuardian(out var guardianId);

        var command = new RegisterProposalTypeCommand(
            Guid.NewGuid(), "PolicyChange", "Changes to governance policies", guardianId, DateTime.UtcNow);

        var (result, domainEvent) = engine.Execute(command);

        Assert.True(result.Success);
        Assert.Equal("PolicyChange", result.ProposalType);
        Assert.Equal(GovernanceProposalTypeAction.Registered, result.Action);
        Assert.NotNull(domainEvent);
        Assert.Equal("PolicyChange", domainEvent!.ProposalType);
        Assert.Equal("Changes to governance policies", domainEvent.Description);
        Assert.Equal(guardianId, domainEvent.RegisteredByGuardianId);
    }

    [Fact]
    public void Execute_RegisterType_Duplicate_Fails()
    {
        var engine = CreateEngineWithGuardian(out var guardianId, seedTypes: new[] { ("Dup", "Desc") });

        var command = new RegisterProposalTypeCommand(
            Guid.NewGuid(), "Dup", "Desc2", guardianId, DateTime.UtcNow);
        var (result, domainEvent) = engine.Execute(command);

        Assert.False(result.Success);
        Assert.Contains("already exists", result.Message);
        Assert.Null(domainEvent);
    }

    [Fact]
    public void Execute_RegisterType_EmptyType_Fails()
    {
        var engine = CreateEngineWithGuardian(out var guardianId);

        var command = new RegisterProposalTypeCommand(
            Guid.NewGuid(), "", "Desc", guardianId, DateTime.UtcNow);
        var (result, domainEvent) = engine.Execute(command);

        Assert.False(result.Success);
        Assert.Contains("required", result.Message);
        Assert.Null(domainEvent);
    }

    [Fact]
    public void Execute_RegisterType_EmptyDescription_Fails()
    {
        var engine = CreateEngineWithGuardian(out var guardianId);

        var command = new RegisterProposalTypeCommand(
            Guid.NewGuid(), "PolicyChange", "", guardianId, DateTime.UtcNow);
        var (result, domainEvent) = engine.Execute(command);

        Assert.False(result.Success);
        Assert.Contains("required", result.Message);
        Assert.Null(domainEvent);
    }

    [Fact]
    public void Execute_RegisterType_InvalidGuardian_Fails()
    {
        var engine = CreateEngineWithGuardian(out _);

        var command = new RegisterProposalTypeCommand(
            Guid.NewGuid(), "PolicyChange", "Desc", Guid.NewGuid(), DateTime.UtcNow);
        var (result, domainEvent) = engine.Execute(command);

        Assert.False(result.Success);
        Assert.Contains("Guardian not found", result.Message);
        Assert.Null(domainEvent);
    }

    [Fact]
    public void Execute_RegisterType_InactiveGuardian_Fails()
    {
        var engine = CreateEngineWithInactiveGuardian(out var guardianId);

        var command = new RegisterProposalTypeCommand(
            Guid.NewGuid(), "PolicyChange", "Desc", guardianId, DateTime.UtcNow);
        var (result, domainEvent) = engine.Execute(command);

        Assert.False(result.Success);
        Assert.Contains("not active", result.Message);
        Assert.Null(domainEvent);
    }

    // --- Deactivation Tests ---

    [Fact]
    public void Execute_DeactivateType_Succeeds()
    {
        var engine = CreateEngineWithGuardian(out var guardianId,
            seedTypes: new[] { ("SystemUpgrade", "System upgrades") });

        var command = new DeactivateProposalTypeCommand(
            Guid.NewGuid(), "SystemUpgrade", "No longer needed", guardianId, DateTime.UtcNow);
        var (result, domainEvent) = engine.Execute(command);

        Assert.True(result.Success);
        Assert.Equal(GovernanceProposalTypeAction.Deactivated, result.Action);
        Assert.NotNull(domainEvent);
        Assert.Equal("SystemUpgrade", domainEvent!.ProposalType);
        Assert.Equal("No longer needed", domainEvent.Reason);
        Assert.Equal(guardianId, domainEvent.DeactivatedByGuardianId);
    }

    [Fact]
    public void Execute_DeactivateType_NotFound_Fails()
    {
        var engine = CreateEngineWithGuardian(out var guardianId);

        var command = new DeactivateProposalTypeCommand(
            Guid.NewGuid(), "Nonexistent", "Reason", guardianId, DateTime.UtcNow);
        var (result, domainEvent) = engine.Execute(command);

        Assert.False(result.Success);
        Assert.Contains("not found", result.Message);
        Assert.Null(domainEvent);
    }

    [Fact]
    public void Execute_DeactivateType_AlreadyDeactivated_Fails()
    {
        var engine = CreateEngineWithGuardian(out var guardianId,
            seedTypes: new[] { ("EmergencyAction", "Emergency actions") });

        engine.Execute(new DeactivateProposalTypeCommand(
            Guid.NewGuid(), "EmergencyAction", "First deactivation", guardianId, DateTime.UtcNow));

        var (result, domainEvent) = engine.Execute(new DeactivateProposalTypeCommand(
            Guid.NewGuid(), "EmergencyAction", "Second deactivation", guardianId, DateTime.UtcNow));

        Assert.False(result.Success);
        Assert.Contains("already deactivated", result.Message);
        Assert.Null(domainEvent);
    }

    [Fact]
    public void Execute_DeactivateType_EmptyReason_Fails()
    {
        var engine = CreateEngineWithGuardian(out var guardianId,
            seedTypes: new[] { ("PolicyChange", "Policy changes") });

        var command = new DeactivateProposalTypeCommand(
            Guid.NewGuid(), "PolicyChange", "", guardianId, DateTime.UtcNow);
        var (result, domainEvent) = engine.Execute(command);

        Assert.False(result.Success);
        Assert.Contains("required", result.Message);
        Assert.Null(domainEvent);
    }

    [Fact]
    public void Execute_DeactivateType_InvalidGuardian_Fails()
    {
        var engine = CreateEngineWithGuardian(out _,
            seedTypes: new[] { ("PolicyChange", "Policy changes") });

        var command = new DeactivateProposalTypeCommand(
            Guid.NewGuid(), "PolicyChange", "Reason", Guid.NewGuid(), DateTime.UtcNow);
        var (result, domainEvent) = engine.Execute(command);

        Assert.False(result.Success);
        Assert.Contains("Guardian not found", result.Message);
        Assert.Null(domainEvent);
    }

    [Fact]
    public void Execute_DeactivateType_InactiveGuardian_Fails()
    {
        var typeStore = new GovernanceProposalTypeStore();
        typeStore.Add(new GovernanceProposalType("PolicyChange", "PolicyChange", "Policy changes"));
        var engine = CreateEngineWithInactiveGuardian(out var guardianId, typeStore);

        var command = new DeactivateProposalTypeCommand(
            Guid.NewGuid(), "PolicyChange", "Reason", guardianId, DateTime.UtcNow);
        var (result, domainEvent) = engine.Execute(command);

        Assert.False(result.Success);
        Assert.Contains("not active", result.Message);
        Assert.Null(domainEvent);
    }

    // --- Validation Tests ---

    [Fact]
    public void Execute_ValidateType_Succeeds()
    {
        var engine = CreateEngineWithGuardian(out var guardianId,
            seedTypes: new[] { ("PolicyChange", "Policy changes") });

        var command = new ValidateProposalTypeCommand(
            Guid.NewGuid(), "PolicyChange", "cluster:WhyceProperty", guardianId, DateTime.UtcNow);
        var (result, domainEvent) = engine.Execute(command);

        Assert.True(result.Success);
        Assert.Equal(GovernanceProposalTypeAction.Validated, result.Action);
        Assert.Equal("cluster:WhyceProperty", result.AuthorityDomain);
        Assert.NotNull(domainEvent);
        Assert.Equal("PolicyChange", domainEvent!.ProposalType);
        Assert.Equal("cluster:WhyceProperty", domainEvent.AuthorityDomain);
        Assert.Equal(guardianId, domainEvent.ValidatedByGuardianId);
    }

    [Fact]
    public void Execute_ValidateType_NotRegistered_Fails()
    {
        var engine = CreateEngineWithGuardian(out var guardianId);

        var command = new ValidateProposalTypeCommand(
            Guid.NewGuid(), "Nonexistent", "global", guardianId, DateTime.UtcNow);
        var (result, domainEvent) = engine.Execute(command);

        Assert.False(result.Success);
        Assert.Contains("not registered", result.Message);
        Assert.Null(domainEvent);
    }

    [Fact]
    public void Execute_ValidateType_Deactivated_Fails()
    {
        var engine = CreateEngineWithGuardian(out var guardianId,
            seedTypes: new[] { ("EmergencyAction", "Emergency actions") });

        engine.Execute(new DeactivateProposalTypeCommand(
            Guid.NewGuid(), "EmergencyAction", "Deactivating", guardianId, DateTime.UtcNow));

        var command = new ValidateProposalTypeCommand(
            Guid.NewGuid(), "EmergencyAction", "global", guardianId, DateTime.UtcNow);
        var (result, domainEvent) = engine.Execute(command);

        Assert.False(result.Success);
        Assert.Contains("deactivated", result.Message);
        Assert.Null(domainEvent);
    }

    [Fact]
    public void Execute_ValidateType_EmptyDomain_Fails()
    {
        var engine = CreateEngineWithGuardian(out var guardianId,
            seedTypes: new[] { ("PolicyChange", "Policy changes") });

        var command = new ValidateProposalTypeCommand(
            Guid.NewGuid(), "PolicyChange", "", guardianId, DateTime.UtcNow);
        var (result, domainEvent) = engine.Execute(command);

        Assert.False(result.Success);
        Assert.Contains("required", result.Message);
        Assert.Null(domainEvent);
    }

    [Fact]
    public void Execute_ValidateType_InvalidGuardian_Fails()
    {
        var engine = CreateEngineWithGuardian(out _,
            seedTypes: new[] { ("PolicyChange", "Policy changes") });

        var command = new ValidateProposalTypeCommand(
            Guid.NewGuid(), "PolicyChange", "global", Guid.NewGuid(), DateTime.UtcNow);
        var (result, domainEvent) = engine.Execute(command);

        Assert.False(result.Success);
        Assert.Contains("Guardian not found", result.Message);
        Assert.Null(domainEvent);
    }

    [Fact]
    public void Execute_ValidateType_InactiveGuardian_Fails()
    {
        var typeStore = new GovernanceProposalTypeStore();
        typeStore.Add(new GovernanceProposalType("PolicyChange", "PolicyChange", "Policy changes"));
        var engine = CreateEngineWithInactiveGuardian(out var guardianId, typeStore);

        var command = new ValidateProposalTypeCommand(
            Guid.NewGuid(), "PolicyChange", "global", guardianId, DateTime.UtcNow);
        var (result, domainEvent) = engine.Execute(command);

        Assert.False(result.Success);
        Assert.Contains("not active", result.Message);
        Assert.Null(domainEvent);
    }

    // --- Query Tests ---

    [Fact]
    public void GetType_Succeeds()
    {
        var engine = CreateEngineWithGuardian(out _,
            seedTypes: new[] { ("system-upgrade", "System upgrades") });

        var type = engine.GetType("system-upgrade");

        Assert.Equal("system-upgrade", type.Name);
    }

    [Fact]
    public void GetType_NotFound_Throws()
    {
        var engine = CreateEngineWithGuardian(out _);

        var ex = Assert.Throws<KeyNotFoundException>(() =>
            engine.GetType("nonexistent"));
        Assert.Contains("Proposal type not found", ex.Message);
    }

    [Fact]
    public void ListTypes_ReturnsAll()
    {
        var engine = CreateEngineWithGuardian(out _, seedTypes: new[]
        {
            ("policy-change", "Policy changes"),
            ("system-upgrade", "System upgrades"),
            ("emergency-action", "Emergency actions"),
            ("dispute-resolution", "Dispute resolutions")
        });

        var types = engine.ListTypes();

        Assert.Equal(4, types.Count);
    }

    [Fact]
    public void ListTypes_Empty_ReturnsEmpty()
    {
        var engine = CreateEngineWithGuardian(out _);

        var types = engine.ListTypes();

        Assert.Empty(types);
    }

    // --- Concurrency Tests ---

    [Fact]
    public async Task ConcurrentRegistration_IsDeterministic()
    {
        var engine = CreateEngineWithGuardian(out var guardianId);

        var tasks = Enumerable.Range(0, 10).Select(i =>
            Task.Run(() => engine.Execute(new RegisterProposalTypeCommand(
                Guid.NewGuid(), $"concurrent-type-{i}", $"Description {i}", guardianId, DateTime.UtcNow))));

        await Task.WhenAll(tasks);

        var types = engine.ListTypes();
        Assert.Equal(10, types.Count);
    }

    [Fact]
    public async Task ConcurrentValidation_AllSucceed()
    {
        var engine = CreateEngineWithGuardian(out var guardianId,
            seedTypes: new[] { ("PolicyChange", "Policy changes") });

        var tasks = Enumerable.Range(0, 10).Select(i =>
            Task.Run(() => engine.Execute(new ValidateProposalTypeCommand(
                Guid.NewGuid(), "PolicyChange", $"domain-{i}", guardianId, DateTime.UtcNow))));

        var results = await Task.WhenAll(tasks);
        Assert.All(results, r => Assert.True(r.Result.Success));
    }

    // --- Architecture Tests ---

    [Fact]
    public void Engine_IsSealed()
    {
        Assert.True(typeof(GovernanceProposalTypeEngine).IsSealed);
    }

    [Fact]
    public void Engine_IsStateless_SharedStoreAccess()
    {
        var typeStore = new GovernanceProposalTypeStore();
        var guardianStore = new GuardianRegistryStore();
        SetupGuardian(guardianStore, out var guardianId);

        var engine1 = new GovernanceProposalTypeEngine(typeStore, guardianStore);
        var engine2 = new GovernanceProposalTypeEngine(typeStore, guardianStore);

        engine1.Execute(new RegisterProposalTypeCommand(
            Guid.NewGuid(), "stateless-test", "Test", guardianId, DateTime.UtcNow));

        var types = engine2.ListTypes();
        Assert.Contains(types, t => t.TypeId == "stateless-test");
    }

    [Fact]
    public void Engine_HasNoPersistenceFields()
    {
        var flags = global::System.Reflection.BindingFlags.Instance
            | global::System.Reflection.BindingFlags.NonPublic
            | global::System.Reflection.BindingFlags.Public;
        var fields = typeof(GovernanceProposalTypeEngine)
            .GetFields(flags)
            .Where(f => !f.Name.Contains("Store"))
            .ToList();

        Assert.Empty(fields);
    }

    [Fact]
    public void Engine_DoesNotReferenceOtherEngines()
    {
        var constructorParams = typeof(GovernanceProposalTypeEngine)
            .GetConstructors()
            .SelectMany(c => c.GetParameters())
            .Select(p => p.ParameterType)
            .ToList();

        Assert.DoesNotContain(constructorParams, t => t.Name.EndsWith("Engine"));
    }

    // --- Event Immutability Tests ---

    [Fact]
    public void RegisteredEvent_IsImmutableRecord()
    {
        Assert.True(typeof(Domain.Events.Governance.GovernanceProposalTypeRegisteredEvent).IsSealed);
    }

    [Fact]
    public void DeactivatedEvent_IsImmutableRecord()
    {
        Assert.True(typeof(Domain.Events.Governance.GovernanceProposalTypeDeactivatedEvent).IsSealed);
    }

    [Fact]
    public void ValidatedEvent_IsImmutableRecord()
    {
        Assert.True(typeof(Domain.Events.Governance.GovernanceProposalTypeValidatedEvent).IsSealed);
    }

    // --- Helpers ---

    private GovernanceProposalTypeEngine CreateEngineWithGuardian(
        out Guid guardianId,
        (string type, string desc)[]? seedTypes = null)
    {
        var typeStore = new GovernanceProposalTypeStore();
        var guardianStore = new GuardianRegistryStore();
        SetupGuardian(guardianStore, out guardianId);

        if (seedTypes is not null)
        {
            foreach (var (type, desc) in seedTypes)
                typeStore.Add(new GovernanceProposalType(type, type, desc));
        }

        return new GovernanceProposalTypeEngine(typeStore, guardianStore);
    }

    private GovernanceProposalTypeEngine CreateEngineWithInactiveGuardian(
        out Guid guardianId,
        GovernanceProposalTypeStore? typeStore = null)
    {
        typeStore ??= new GovernanceProposalTypeStore();
        var guardianStore = new GuardianRegistryStore();
        var identityRegistry = new IdentityRegistry();
        var identityId = Guid.NewGuid();
        identityRegistry.Register(new IdentityAggregate(IdentityId.From(identityId), IdentityType.User));

        guardianId = Guid.NewGuid();
        var guardianEngine = new GuardianRegistryEngine(guardianStore, identityRegistry);
        guardianEngine.RegisterGuardian(guardianId.ToString(), identityId, "InactiveGuardian", new List<string>());
        // Guardian stays in Registered status, not activated

        return new GovernanceProposalTypeEngine(typeStore, guardianStore);
    }

    private static void SetupGuardian(GuardianRegistryStore guardianStore, out Guid guardianId)
    {
        var identityRegistry = new IdentityRegistry();
        var identityId = Guid.NewGuid();
        identityRegistry.Register(new IdentityAggregate(IdentityId.From(identityId), IdentityType.User));

        guardianId = Guid.NewGuid();
        var guardianEngine = new GuardianRegistryEngine(guardianStore, identityRegistry);
        guardianEngine.RegisterGuardian(guardianId.ToString(), identityId, "TestGuardian", new List<string>());
        guardianStore.ActivateGuardian(guardianId.ToString());
    }
}
