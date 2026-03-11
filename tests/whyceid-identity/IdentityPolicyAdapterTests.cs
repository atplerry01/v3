using Whycespace.Engines.T0U.WhyceID;
using Whycespace.System.WhyceID.Aggregates;
using Whycespace.System.WhyceID.Models;
using Whycespace.System.WhyceID.Registry;
using Whycespace.System.WhyceID.Stores;

namespace Whycespace.WhyceID.Identity.Tests;

public class IdentityPolicyAdapterTests
{
    private readonly IdentityRegistry _registry;
    private readonly IdentityRoleStore _roleStore;
    private readonly IdentityTrustStore _trustStore;
    private readonly IdentityRevocationStore _revocationStore;
    private readonly IdentityPolicyEnforcementAdapter _adapter;

    public IdentityPolicyAdapterTests()
    {
        _registry = new IdentityRegistry();
        _roleStore = new IdentityRoleStore();
        _trustStore = new IdentityTrustStore();
        _revocationStore = new IdentityRevocationStore();
        _adapter = new IdentityPolicyEnforcementAdapter(
            _registry, _roleStore, _trustStore, _revocationStore);
    }

    private Guid RegisterVerifiedIdentity(int trustScore = 80)
    {
        var id = IdentityId.New();
        var identity = new IdentityAggregate(id, IdentityType.User);
        identity.Verify();
        _registry.Register(identity);
        _trustStore.Update(id.Value, new IdentityTrustScore(trustScore, DateTime.UtcNow));
        return id.Value;
    }

    [Fact]
    public void EvaluateIdentityAccess_VerifiedHighTrust_Allowed()
    {
        var identityId = RegisterVerifiedIdentity(80);

        var result = _adapter.EvaluateIdentityAccess(identityId);

        Assert.True(result);
    }

    [Fact]
    public void EvaluateIdentityAccess_NotVerified_Denied()
    {
        var id = IdentityId.New();
        var identity = new IdentityAggregate(id, IdentityType.User);
        _registry.Register(identity);
        _trustStore.Update(id.Value, new IdentityTrustScore(80, DateTime.UtcNow));

        var result = _adapter.EvaluateIdentityAccess(id.Value);

        Assert.False(result);
    }

    [Fact]
    public void EvaluateIdentityAccess_MissingIdentity_Throws()
    {
        var missingId = Guid.NewGuid();

        var ex = Assert.Throws<InvalidOperationException>(
            () => _adapter.EvaluateIdentityAccess(missingId));

        Assert.Contains("does not exist", ex.Message);
    }

    [Fact]
    public void EvaluateIdentityAccess_RevokedIdentity_Denied()
    {
        var identityId = RegisterVerifiedIdentity(80);
        _revocationStore.Register(new IdentityRevocation(
            Guid.NewGuid(), identityId, "security_breach", DateTime.UtcNow, true));

        var result = _adapter.EvaluateIdentityAccess(identityId);

        Assert.False(result);
    }

    [Fact]
    public void EvaluateIdentityAccess_LowTrustScore_Denied()
    {
        var identityId = RegisterVerifiedIdentity(20);

        var result = _adapter.EvaluateIdentityAccess(identityId);

        Assert.False(result);
    }

    [Fact]
    public void BuildContext_ReturnsCorrectContext()
    {
        var identityId = RegisterVerifiedIdentity(75);
        _roleStore.Assign(identityId, "admin");
        _roleStore.Assign(identityId, "operator");

        var context = _adapter.BuildContext(identityId);

        Assert.Equal(identityId, context.IdentityId);
        Assert.Equal(2, context.Roles.Count);
        Assert.Equal(75, context.TrustScore);
        Assert.True(context.Verified);
        Assert.False(context.Revoked);
    }
}
