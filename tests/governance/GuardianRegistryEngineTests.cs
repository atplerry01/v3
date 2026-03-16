using Whycespace.Engines.T0U.Governance;
using Whycespace.Systems.Upstream.Governance.Models;
using Whycespace.Systems.Upstream.Governance.Stores;
using Whycespace.Systems.WhyceID.Aggregates;
using Whycespace.Systems.WhyceID.Models;
using Whycespace.Systems.WhyceID.Registry;

namespace Whycespace.Governance.Tests;

public class GuardianRegistryEngineTests
{
    private readonly IdentityRegistry _identityRegistry = new();
    private readonly GuardianRegistryStore _store = new();
    private readonly GuardianRegistryEngine _engine;
    private readonly Guid _identityId;

    public GuardianRegistryEngineTests()
    {
        _engine = new GuardianRegistryEngine(_store, _identityRegistry);

        _identityId = Guid.NewGuid();
        var identity = new IdentityAggregate(IdentityId.From(_identityId), IdentityType.User);
        _identityRegistry.Register(identity);
    }

    [Fact]
    public void RegisterGuardian_Succeeds()
    {
        var guardian = _engine.RegisterGuardian("g-1", _identityId, "Alice", new List<string> { "constitutional", "dispute" });

        Assert.Equal("g-1", guardian.GuardianId);
        Assert.Equal(_identityId, guardian.IdentityId);
        Assert.Equal("Alice", guardian.Name);
        Assert.Equal(GuardianStatus.Registered, guardian.Status);
        Assert.Contains("constitutional", guardian.Roles);
        Assert.Contains("dispute", guardian.Roles);
        Assert.Null(guardian.ActivatedAt);
    }

    [Fact]
    public void RegisterGuardian_DuplicateId_Throws()
    {
        _engine.RegisterGuardian("g-dup", _identityId, "Alice", new List<string> { "constitutional" });

        var ex = Assert.Throws<InvalidOperationException>(() =>
            _engine.RegisterGuardian("g-dup", _identityId, "Bob", new List<string> { "dispute" }));
        Assert.Contains("already registered", ex.Message);
    }

    [Fact]
    public void RegisterGuardian_InvalidIdentity_Throws()
    {
        var fakeId = Guid.NewGuid();

        var ex = Assert.Throws<InvalidOperationException>(() =>
            _engine.RegisterGuardian("g-bad", fakeId, "Ghost", new List<string> { "constitutional" }));
        Assert.Contains("Identity does not exist", ex.Message);
    }

    [Fact]
    public void ActivateGuardian_Succeeds()
    {
        _engine.RegisterGuardian("g-act", _identityId, "Alice", new List<string> { "constitutional" });

        var guardian = _engine.ActivateGuardian("g-act");

        Assert.Equal(GuardianStatus.Active, guardian.Status);
        Assert.NotNull(guardian.ActivatedAt);
    }

    [Fact]
    public void DeactivateGuardian_Succeeds()
    {
        _engine.RegisterGuardian("g-deact", _identityId, "Alice", new List<string> { "constitutional" });
        _engine.ActivateGuardian("g-deact");

        var guardian = _engine.DeactivateGuardian("g-deact");

        Assert.Equal(GuardianStatus.Inactive, guardian.Status);
    }

    [Fact]
    public void ListGuardians_ReturnsAll()
    {
        _engine.RegisterGuardian("g-a", _identityId, "Alice", new List<string> { "constitutional" });
        _engine.RegisterGuardian("g-b", _identityId, "Bob", new List<string> { "dispute" });

        var guardians = _engine.ListGuardians();

        Assert.Equal(2, guardians.Count);
    }

    [Fact]
    public void GetGuardian_NotFound_Throws()
    {
        var ex = Assert.Throws<KeyNotFoundException>(() =>
            _engine.GetGuardian("nonexistent"));
        Assert.Contains("not found", ex.Message);
    }
}
