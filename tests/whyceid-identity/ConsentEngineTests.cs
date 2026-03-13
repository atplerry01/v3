using Whycespace.Engines.T0U.WhyceID;
using Whycespace.System.WhyceID.Aggregates;
using Whycespace.System.WhyceID.Models;
using Whycespace.System.WhyceID.Registry;
using Whycespace.System.WhyceID.Stores;

namespace Whycespace.WhyceID.Identity.Tests;

public class ConsentEngineTests
{
    private readonly IdentityRegistry _registry;
    private readonly IdentityConsentStore _store;
    private readonly ConsentEngine _engine;

    public ConsentEngineTests()
    {
        _registry = new IdentityRegistry();
        _store = new IdentityConsentStore();
        _engine = new ConsentEngine(_registry, _store);
    }

    private Guid RegisterIdentity()
    {
        var id = IdentityId.New();
        var identity = new IdentityAggregate(id, IdentityType.User);
        _registry.Register(identity);
        return id.Value;
    }

    [Fact]
    public void GrantConsent_ShouldSucceed()
    {
        var identityId = RegisterIdentity();

        var consent = _engine.GrantConsent(identityId, "whyceproperty", "identity:read");

        Assert.NotEqual(Guid.Empty, consent.ConsentId);
        Assert.Equal(identityId, consent.IdentityId);
        Assert.Equal("whyceproperty", consent.Target);
        Assert.Equal("identity:read", consent.Scope);
        Assert.False(consent.Revoked);
    }

    [Fact]
    public void GrantConsent_MissingIdentity_ShouldThrow()
    {
        Assert.Throws<InvalidOperationException>(() =>
            _engine.GrantConsent(Guid.NewGuid(), "whyceproperty", "identity:read"));
    }

    [Fact]
    public void GrantConsent_EmptyTarget_ShouldThrow()
    {
        var identityId = RegisterIdentity();

        Assert.Throws<ArgumentException>(() =>
            _engine.GrantConsent(identityId, "", "identity:read"));
    }

    [Fact]
    public void GrantConsent_EmptyScope_ShouldThrow()
    {
        var identityId = RegisterIdentity();

        Assert.Throws<ArgumentException>(() =>
            _engine.GrantConsent(identityId, "whyceproperty", ""));
    }

    [Fact]
    public void CheckConsent_ShouldReturnTrue_WhenGranted()
    {
        var identityId = RegisterIdentity();
        _engine.GrantConsent(identityId, "whyceproperty", "identity:read");

        Assert.True(_engine.CheckConsent(identityId, "whyceproperty", "identity:read"));
    }

    [Fact]
    public void CheckConsent_ShouldReturnFalse_WhenNotGranted()
    {
        var identityId = RegisterIdentity();

        Assert.False(_engine.CheckConsent(identityId, "whyceproperty", "identity:read"));
    }

    [Fact]
    public void RevokeConsent_ShouldInvalidateConsent()
    {
        var identityId = RegisterIdentity();
        var consent = _engine.GrantConsent(identityId, "whyceproperty", "identity:read");

        _engine.RevokeConsent(consent.ConsentId);

        Assert.False(_engine.CheckConsent(identityId, "whyceproperty", "identity:read"));
    }

    [Fact]
    public void GetConsents_ShouldReturnMultipleConsents()
    {
        var identityId = RegisterIdentity();
        _engine.GrantConsent(identityId, "whyceproperty", "identity:read");
        _engine.GrantConsent(identityId, "whycemobility", "identity:profile");
        _engine.GrantConsent(identityId, "externalbankapi", "identity:financial");

        var consents = _engine.GetConsents(identityId);
        Assert.Equal(3, consents.Count);
    }

    [Fact]
    public void GetConsents_UnknownIdentity_ShouldReturnEmpty()
    {
        var consents = _engine.GetConsents(Guid.NewGuid());
        Assert.Empty(consents);
    }
}
