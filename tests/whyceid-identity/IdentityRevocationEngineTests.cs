using Whycespace.Engines.T0U.WhyceID;
using Whycespace.System.WhyceID.Aggregates;
using Whycespace.System.WhyceID.Models;
using Whycespace.System.WhyceID.Registry;
using Whycespace.System.WhyceID.Stores;

namespace Whycespace.WhyceID.Identity.Tests;

public class IdentityRevocationEngineTests
{
    private readonly IdentityRegistry _registry;
    private readonly IdentityRevocationStore _store;
    private readonly IdentityRevocationEngine _engine;

    public IdentityRevocationEngineTests()
    {
        _registry = new IdentityRegistry();
        _store = new IdentityRevocationStore();
        _engine = new IdentityRevocationEngine(_registry, _store);
    }

    private Guid RegisterIdentity()
    {
        var id = IdentityId.New();
        var identity = new IdentityAggregate(id, IdentityType.User);
        _registry.Register(identity);
        return id.Value;
    }

    [Fact]
    public void RevokeIdentity_ValidIdentity_ReturnsRevocation()
    {
        var identityId = RegisterIdentity();

        var revocation = _engine.RevokeIdentity(identityId, "security_breach");

        Assert.NotEqual(Guid.Empty, revocation.RevocationId);
        Assert.Equal(identityId, revocation.IdentityId);
        Assert.Equal("security_breach", revocation.Reason);
        Assert.True(revocation.Active);
    }

    [Fact]
    public void RevokeIdentity_MissingIdentity_Throws()
    {
        var missingId = Guid.NewGuid();

        var ex = Assert.Throws<InvalidOperationException>(
            () => _engine.RevokeIdentity(missingId, "policy_violation"));

        Assert.Contains("does not exist", ex.Message);
    }

    [Fact]
    public void RevokeIdentity_EmptyReason_Throws()
    {
        var identityId = RegisterIdentity();

        Assert.Throws<ArgumentException>(
            () => _engine.RevokeIdentity(identityId, ""));
    }

    [Fact]
    public void IsIdentityRevoked_AfterRevocation_ReturnsTrue()
    {
        var identityId = RegisterIdentity();
        _engine.RevokeIdentity(identityId, "governance_action");

        var result = _engine.IsIdentityRevoked(identityId);

        Assert.True(result);
    }

    [Fact]
    public void IsIdentityRevoked_NoRevocation_ReturnsFalse()
    {
        var identityId = RegisterIdentity();

        var result = _engine.IsIdentityRevoked(identityId);

        Assert.False(result);
    }

    [Fact]
    public void MultipleRevocations_AllRecorded()
    {
        var identityId = RegisterIdentity();
        _engine.RevokeIdentity(identityId, "security_breach");
        _engine.RevokeIdentity(identityId, "policy_violation");

        var revocations = _engine.GetRevocations(identityId);

        Assert.Equal(2, revocations.Count);
    }

    [Fact]
    public void GetAllRevocations_ReturnsAll()
    {
        var id1 = RegisterIdentity();
        var id2 = RegisterIdentity();
        _engine.RevokeIdentity(id1, "security_breach");
        _engine.RevokeIdentity(id2, "legal_enforcement");

        var all = _engine.GetAllRevocations();

        Assert.Equal(2, all.Count);
    }
}
