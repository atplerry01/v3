using Whycespace.Engines.T0U.WhyceGovernance;
using Whycespace.Systems.Upstream.Governance.Models;
using Whycespace.Systems.Upstream.Governance.Stores;
using Whycespace.Systems.WhyceID.Aggregates;
using Whycespace.Systems.WhyceID.Models;
using Whycespace.Systems.WhyceID.Registry;

namespace Whycespace.Governance.Tests;

public class GovernanceDelegationEngineTests
{
    private readonly IdentityRegistry _identityRegistry = new();
    private readonly GuardianRegistryStore _guardianStore = new();
    private readonly GovernanceRoleStore _roleStore = new();
    private readonly GovernanceDelegationStore _delegationStore = new();
    private readonly GovernanceDelegationEngine _engine;
    private readonly GovernanceRoleEngine _roleEngine;
    private readonly GuardianRegistryEngine _guardianEngine;

    public GovernanceDelegationEngineTests()
    {
        _engine = new GovernanceDelegationEngine(_delegationStore, _guardianStore, _roleStore);
        _roleEngine = new GovernanceRoleEngine(_roleStore, _guardianStore);
        _guardianEngine = new GuardianRegistryEngine(_guardianStore, _identityRegistry);

        var identityId = Guid.NewGuid();
        var identity = new IdentityAggregate(IdentityId.From(identityId), IdentityType.User);
        _identityRegistry.Register(identity);

        _guardianEngine.RegisterGuardian("g-alice", identityId, "Alice", new List<string>());
        _guardianEngine.RegisterGuardian("g-bob", identityId, "Bob", new List<string>());

        _roleStore.AddRole(new GovernanceRole("council", "Council", "Council role", new List<string> { "vote", "propose" }));
        _roleStore.AssignRole("g-alice", "council");
    }

    [Fact]
    public void CreateDelegation_Succeeds()
    {
        var start = DateTime.UtcNow;
        var end = start.AddDays(7);

        var delegation = _engine.CreateDelegation("d-1", "g-alice", "g-bob", "council", start, end);

        Assert.Equal("d-1", delegation.DelegationId);
        Assert.Equal("g-alice", delegation.FromGuardian);
        Assert.Equal("g-bob", delegation.ToGuardian);
        Assert.Equal("council", delegation.RoleScope);
        Assert.Equal(DelegationStatus.Active, delegation.Status);
    }

    [Fact]
    public void CreateDelegation_MustExpire()
    {
        var now = DateTime.UtcNow;

        var ex = Assert.Throws<InvalidOperationException>(() =>
            _engine.CreateDelegation("d-bad", "g-alice", "g-bob", "council", now, now));
        Assert.Contains("valid expiration", ex.Message);
    }

    [Fact]
    public void CreateDelegation_CannotExceedRoleAuthority()
    {
        _roleStore.AddRole(new GovernanceRole("admin", "Admin", "Admin role", new List<string> { "all" }));

        var ex = Assert.Throws<InvalidOperationException>(() =>
            _engine.CreateDelegation("d-bad", "g-alice", "g-bob", "admin", DateTime.UtcNow, DateTime.UtcNow.AddDays(1)));
        Assert.Contains("does not hold role", ex.Message);
    }

    [Fact]
    public void CreateDelegation_CannotDelegateToSelf()
    {
        var ex = Assert.Throws<InvalidOperationException>(() =>
            _engine.CreateDelegation("d-self", "g-alice", "g-alice", "council", DateTime.UtcNow, DateTime.UtcNow.AddDays(1)));
        Assert.Contains("Cannot delegate to self", ex.Message);
    }

    [Fact]
    public void CreateDelegation_InvalidGuardian_Throws()
    {
        var ex = Assert.Throws<KeyNotFoundException>(() =>
            _engine.CreateDelegation("d-bad", "nonexistent", "g-bob", "council", DateTime.UtcNow, DateTime.UtcNow.AddDays(1)));
        Assert.Contains("Guardian not found", ex.Message);
    }

    [Fact]
    public void RevokeDelegation_Succeeds()
    {
        _engine.CreateDelegation("d-rev", "g-alice", "g-bob", "council", DateTime.UtcNow, DateTime.UtcNow.AddDays(7));

        var revoked = _engine.RevokeDelegation("d-rev");

        Assert.Equal(DelegationStatus.Revoked, revoked.Status);
    }

    [Fact]
    public void RevokeDelegation_AlreadyRevoked_Throws()
    {
        _engine.CreateDelegation("d-rev2", "g-alice", "g-bob", "council", DateTime.UtcNow, DateTime.UtcNow.AddDays(7));
        _engine.RevokeDelegation("d-rev2");

        var ex = Assert.Throws<InvalidOperationException>(() =>
            _engine.RevokeDelegation("d-rev2"));
        Assert.Contains("already revoked", ex.Message);
    }

    [Fact]
    public void GetDelegations_ReturnsDelegationsForGuardian()
    {
        _engine.CreateDelegation("d-get1", "g-alice", "g-bob", "council", DateTime.UtcNow, DateTime.UtcNow.AddDays(7));

        var fromAlice = _engine.GetDelegations("g-alice");
        var fromBob = _engine.GetDelegations("g-bob");

        Assert.Single(fromAlice);
        Assert.Single(fromBob);
        Assert.Equal("d-get1", fromAlice[0].DelegationId);
    }

    [Fact]
    public void GetDelegations_InvalidGuardian_Throws()
    {
        var ex = Assert.Throws<KeyNotFoundException>(() =>
            _engine.GetDelegations("nonexistent"));
        Assert.Contains("Guardian not found", ex.Message);
    }
}
