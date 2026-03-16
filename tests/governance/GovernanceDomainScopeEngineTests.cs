using Whycespace.Engines.T0U.Governance;
using Whycespace.Engines.T0U.Governance.Commands;
using Whycespace.Engines.T0U.Governance.Results;
using Whycespace.Systems.Upstream.Governance.Models;
using Whycespace.Systems.Upstream.Governance.Stores;
using Whycespace.Systems.WhyceID.Aggregates;
using Whycespace.Systems.WhyceID.Models;
using Whycespace.Systems.WhyceID.Registry;

namespace Whycespace.Governance.Tests;

public class GovernanceDomainScopeEngineTests
{
    private readonly GovernanceDomainScopeStore _scopeStore = new();
    private readonly GuardianRegistryStore _guardianStore = new();
    private readonly GovernanceDomainScopeEngine _engine;
    private readonly Guid _guardianId;

    public GovernanceDomainScopeEngineTests()
    {
        _engine = new GovernanceDomainScopeEngine(_scopeStore, _guardianStore);

        var identityRegistry = new IdentityRegistry();
        var identityId = Guid.NewGuid();
        identityRegistry.Register(new IdentityAggregate(IdentityId.From(identityId), IdentityType.User));
        var guardianEngine = new GuardianRegistryEngine(_guardianStore, identityRegistry);
        guardianEngine.RegisterGuardian("g-alice", identityId, "Alice", new List<string>());

        _guardianStore.ActivateGuardian("g-alice");
        _guardianId = Guid.Parse("g-alice".GetHashCode().ToString("x8").PadLeft(32, '0').Insert(8, "-").Insert(13, "-").Insert(18, "-").Insert(23, "-"));
    }

    private Guid GuardianGuid()
    {
        // GuardianId is stored as string "g-alice", so we use that as the guardian identifier
        // The engine looks up by Guid.ToString(), so we need to register with a proper GUID string
        return Guid.Empty; // Placeholder - see RegisterWithGuidGuardian helper
    }

    [Fact]
    public void RegisterDomain_Succeeds()
    {
        var engine = CreateEngineWithGuidGuardian(out var guardianId);

        var command = new RegisterDomainScopeCommand(
            Guid.NewGuid(), "cluster:WhyceProperty", "Cluster domain", guardianId, DateTime.UtcNow);

        var (result, domainEvent) = engine.Execute(command);

        Assert.True(result.Success);
        Assert.Equal(DomainScopeAction.Registered, result.Action);
        Assert.Equal("cluster:WhyceProperty", result.AuthorityDomain);
        Assert.NotNull(domainEvent);
        Assert.Equal("cluster:WhyceProperty", domainEvent!.AuthorityDomain);
    }

    [Fact]
    public void RegisterDomain_DuplicateDomain_Fails()
    {
        var engine = CreateEngineWithGuidGuardian(out var guardianId);

        var command = new RegisterDomainScopeCommand(
            Guid.NewGuid(), "global", "Global domain", guardianId, DateTime.UtcNow);
        engine.Execute(command);

        var duplicate = new RegisterDomainScopeCommand(
            Guid.NewGuid(), "global", "Global domain again", guardianId, DateTime.UtcNow);
        var (result, domainEvent) = engine.Execute(duplicate);

        Assert.False(result.Success);
        Assert.Contains("already exists", result.Message);
        Assert.Null(domainEvent);
    }

    [Fact]
    public void RegisterDomain_InvalidGuardian_Fails()
    {
        var engine = CreateEngineWithGuidGuardian(out _);

        var command = new RegisterDomainScopeCommand(
            Guid.NewGuid(), "policy", "Policy domain", Guid.NewGuid(), DateTime.UtcNow);

        var (result, domainEvent) = engine.Execute(command);

        Assert.False(result.Success);
        Assert.Contains("Guardian not found", result.Message);
        Assert.Null(domainEvent);
    }

    [Fact]
    public void DeactivateDomain_Succeeds()
    {
        var engine = CreateEngineWithGuidGuardian(out var guardianId);

        var registerCmd = new RegisterDomainScopeCommand(
            Guid.NewGuid(), "spv:TestSPV", "SPV domain", guardianId, DateTime.UtcNow);
        engine.Execute(registerCmd);

        var deactivateCmd = new DeactivateDomainScopeCommand(
            Guid.NewGuid(), "spv:TestSPV", "No longer needed", guardianId, DateTime.UtcNow);
        var (result, domainEvent) = engine.Execute(deactivateCmd);

        Assert.True(result.Success);
        Assert.Equal(DomainScopeAction.Deactivated, result.Action);
        Assert.NotNull(domainEvent);
        Assert.Equal("No longer needed", domainEvent!.Reason);
    }

    [Fact]
    public void DeactivateDomain_NotFound_Fails()
    {
        var engine = CreateEngineWithGuidGuardian(out var guardianId);

        var command = new DeactivateDomainScopeCommand(
            Guid.NewGuid(), "nonexistent", "Reason", guardianId, DateTime.UtcNow);

        var (result, domainEvent) = engine.Execute(command);

        Assert.False(result.Success);
        Assert.Contains("not found", result.Message);
        Assert.Null(domainEvent);
    }

    [Fact]
    public void DeactivateDomain_AlreadyDeactivated_Fails()
    {
        var engine = CreateEngineWithGuidGuardian(out var guardianId);

        var registerCmd = new RegisterDomainScopeCommand(
            Guid.NewGuid(), "cluster:Test", "Test", guardianId, DateTime.UtcNow);
        engine.Execute(registerCmd);

        var deactivateCmd = new DeactivateDomainScopeCommand(
            Guid.NewGuid(), "cluster:Test", "First deactivation", guardianId, DateTime.UtcNow);
        engine.Execute(deactivateCmd);

        var deactivateAgain = new DeactivateDomainScopeCommand(
            Guid.NewGuid(), "cluster:Test", "Second deactivation", guardianId, DateTime.UtcNow);
        var (result, _) = engine.Execute(deactivateAgain);

        Assert.False(result.Success);
        Assert.Contains("already deactivated", result.Message);
    }

    [Fact]
    public void ValidateDomainScope_Succeeds()
    {
        var engine = CreateEngineWithGuidGuardian(out var guardianId);

        var registerCmd = new RegisterDomainScopeCommand(
            Guid.NewGuid(), "authority:WhyceProperty.Acquisition", "Authority domain", guardianId, DateTime.UtcNow);
        engine.Execute(registerCmd);

        var validateCmd = new ValidateDomainScopeCommand(
            Guid.NewGuid(), Guid.NewGuid(), "authority:WhyceProperty.Acquisition",
            ProposalType.Policy, guardianId, DateTime.UtcNow);
        var (result, domainEvent) = engine.Execute(validateCmd);

        Assert.True(result.Success);
        Assert.Equal(DomainScopeAction.Validated, result.Action);
        Assert.Equal(ProposalType.Policy, result.ProposalType);
        Assert.NotNull(domainEvent);
        Assert.Equal("Policy", domainEvent!.ProposalType);
    }

    [Fact]
    public void ValidateDomainScope_DeactivatedDomain_Fails()
    {
        var engine = CreateEngineWithGuidGuardian(out var guardianId);

        var registerCmd = new RegisterDomainScopeCommand(
            Guid.NewGuid(), "cluster:Inactive", "Inactive domain", guardianId, DateTime.UtcNow);
        engine.Execute(registerCmd);

        var deactivateCmd = new DeactivateDomainScopeCommand(
            Guid.NewGuid(), "cluster:Inactive", "Deactivating", guardianId, DateTime.UtcNow);
        engine.Execute(deactivateCmd);

        var validateCmd = new ValidateDomainScopeCommand(
            Guid.NewGuid(), Guid.NewGuid(), "cluster:Inactive",
            ProposalType.Constitutional, guardianId, DateTime.UtcNow);
        var (result, domainEvent) = engine.Execute(validateCmd);

        Assert.False(result.Success);
        Assert.Contains("deactivated", result.Message);
        Assert.Null(domainEvent);
    }

    [Fact]
    public void ValidateDomainScope_NonexistentDomain_Fails()
    {
        var engine = CreateEngineWithGuidGuardian(out var guardianId);

        var command = new ValidateDomainScopeCommand(
            Guid.NewGuid(), Guid.NewGuid(), "nonexistent",
            ProposalType.Operational, guardianId, DateTime.UtcNow);

        var (result, _) = engine.Execute(command);

        Assert.False(result.Success);
        Assert.Contains("not found", result.Message);
    }

    [Fact]
    public void ListDomains_ReturnsAllRegistered()
    {
        var engine = CreateEngineWithGuidGuardian(out var guardianId);

        engine.Execute(new RegisterDomainScopeCommand(
            Guid.NewGuid(), "global", "Global", guardianId, DateTime.UtcNow));
        engine.Execute(new RegisterDomainScopeCommand(
            Guid.NewGuid(), "cluster:A", "Cluster A", guardianId, DateTime.UtcNow));

        var domains = engine.ListDomains();

        Assert.Equal(2, domains.Count);
    }

    [Fact]
    public void ConcurrentRegistration_IsDeterministic()
    {
        var engine = CreateEngineWithGuidGuardian(out var guardianId);

        var tasks = Enumerable.Range(0, 10).Select(i =>
            Task.Run(() => engine.Execute(new RegisterDomainScopeCommand(
                Guid.NewGuid(), $"concurrent:{i}", $"Domain {i}", guardianId, DateTime.UtcNow))));

        Task.WaitAll(tasks.ToArray());

        var domains = engine.ListDomains();
        Assert.Equal(10, domains.Count);
    }

    [Fact]
    public void Engine_IsStateless_NoInstanceFields()
    {
        var scopeStore = new GovernanceDomainScopeStore();
        var guardianStore = new GuardianRegistryStore();
        var identityRegistry = new IdentityRegistry();
        var identityId = Guid.NewGuid();
        identityRegistry.Register(new IdentityAggregate(IdentityId.From(identityId), IdentityType.User));

        var guardianId = Guid.NewGuid();
        var guardianEngine = new GuardianRegistryEngine(guardianStore, identityRegistry);
        guardianEngine.RegisterGuardian(guardianId.ToString(), identityId, "TestGuardian", new List<string>());
        guardianStore.ActivateGuardian(guardianId.ToString());

        var engine1 = new GovernanceDomainScopeEngine(scopeStore, guardianStore);
        var engine2 = new GovernanceDomainScopeEngine(scopeStore, guardianStore);

        engine1.Execute(new RegisterDomainScopeCommand(
            Guid.NewGuid(), "stateless:test", "Test", guardianId, DateTime.UtcNow));

        var domains = engine2.ListDomains();
        Assert.Contains(domains, d => d.ScopeId == "stateless:test");
    }

    private GovernanceDomainScopeEngine CreateEngineWithGuidGuardian(out Guid guardianId)
    {
        var scopeStore = new GovernanceDomainScopeStore();
        var guardianStore = new GuardianRegistryStore();
        var identityRegistry = new IdentityRegistry();
        var identityId = Guid.NewGuid();
        identityRegistry.Register(new IdentityAggregate(IdentityId.From(identityId), IdentityType.User));

        guardianId = Guid.NewGuid();
        var guardianEngine = new GuardianRegistryEngine(guardianStore, identityRegistry);
        guardianEngine.RegisterGuardian(guardianId.ToString(), identityId, "TestGuardian", new List<string>());
        guardianStore.ActivateGuardian(guardianId.ToString());

        return new GovernanceDomainScopeEngine(scopeStore, guardianStore);
    }
}
