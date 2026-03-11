using Whycespace.Engines.T0U.WhyceID;
using Whycespace.System.WhyceID.Aggregates;
using Whycespace.System.WhyceID.Models;
using Whycespace.System.WhyceID.Registry;

namespace Whycespace.WhyceID.Identity.Tests;

public class IdentityVerificationTests
{
    private readonly IdentityRegistry _registry;
    private readonly IdentityVerificationEngine _engine;

    public IdentityVerificationTests()
    {
        _registry = new IdentityRegistry();
        _engine = new IdentityVerificationEngine(_registry);
    }

    private Guid RegisterIdentity()
    {
        var id = IdentityId.New();
        var identity = new IdentityAggregate(id, IdentityType.User);
        _registry.Register(identity);
        return id.Value;
    }

    [Fact]
    public void VerifyIdentity_ShouldSucceed()
    {
        var identityId = RegisterIdentity();

        _engine.VerifyIdentity(identityId);

        var identity = _registry.Get(identityId);
        Assert.Equal(IdentityStatus.Verified, identity.Status);
    }

    [Fact]
    public void VerifyIdentity_MissingIdentity_ShouldThrow()
    {
        Assert.Throws<InvalidOperationException>(() =>
            _engine.VerifyIdentity(Guid.NewGuid()));
    }

    [Fact]
    public void VerifyIdentity_AlreadyVerified_ShouldThrow()
    {
        var identityId = RegisterIdentity();
        _engine.VerifyIdentity(identityId);

        Assert.Throws<InvalidOperationException>(() =>
            _engine.VerifyIdentity(identityId));
    }

    [Fact]
    public void VerifyIdentity_RevokedIdentity_ShouldThrow()
    {
        var identityId = RegisterIdentity();
        var identity = _registry.Get(identityId);
        identity.Revoke();
        _registry.Update(identity);

        Assert.Throws<InvalidOperationException>(() =>
            _engine.VerifyIdentity(identityId));
    }

    [Fact]
    public void VerifyIdentity_ShouldPersistInRegistry()
    {
        var identityId = RegisterIdentity();

        _engine.VerifyIdentity(identityId);

        var updated = _registry.Get(identityId);
        Assert.Equal(IdentityStatus.Verified, updated.Status);
    }

    [Fact]
    public void VerifyIdentity_ShouldSetVerificationTimestamp()
    {
        var identityId = RegisterIdentity();

        _engine.VerifyIdentity(identityId);

        var identity = _registry.Get(identityId);
        Assert.NotNull(identity.VerifiedAt);
    }
}
