using Whycespace.Engines.T0U.WhyceID.Identity.Creation;
using Whycespace.Engines.T0U.WhyceID.Identity.Attributes;
using Whycespace.Engines.T0U.WhyceID.Identity.Graph;
using Whycespace.Engines.T0U.WhyceID.Authentication;
using Whycespace.Engines.T0U.WhyceID.Authorization.Decision;
using Whycespace.Engines.T0U.WhyceID.Consent;
using Whycespace.Engines.T0U.WhyceID.Trust.Device;
using Whycespace.Engines.T0U.WhyceID.Trust.Scoring;
using Whycespace.Engines.T0U.WhyceID.Federation.Provider;
using Whycespace.Engines.T0U.WhyceID.AccessScope.Assignment;
using Whycespace.Engines.T0U.WhyceID.Audit.Reporting;
using Whycespace.Engines.T0U.WhyceID.Recovery.Execution;
using Whycespace.Engines.T0U.WhyceID.Revocation.Execution;
using Whycespace.Engines.T0U.WhyceID.Roles.Assignment;
using Whycespace.Engines.T0U.WhyceID.Permissions.Grant;
using Whycespace.Engines.T0U.WhyceID.Policy.Enforcement;
using Whycespace.Engines.T0U.WhyceID.Verification.Identity;
using Whycespace.Engines.T0U.WhyceID.Service.Registration;
using Whycespace.Engines.T0U.WhyceID.Session.Creation;
using Whycespace.Systems.WhyceID.Aggregates;
using Whycespace.Systems.WhyceID.Models;
using Whycespace.Systems.WhyceID.Registry;
using Whycespace.Systems.WhyceID.Stores;

namespace Whycespace.WhyceID.Identity.Tests;

public class FederationEngineTests
{
    private readonly IdentityRegistry _registry;
    private readonly IdentityFederationStore _store;
    private readonly FederationEngine _engine;

    public FederationEngineTests()
    {
        _registry = new IdentityRegistry();
        _store = new IdentityFederationStore();
        _engine = new FederationEngine(_registry, _store);
    }

    private Guid RegisterIdentity()
    {
        var id = IdentityId.New();
        var identity = new IdentityAggregate(id, IdentityType.User);
        _registry.Register(identity);
        return id.Value;
    }

    [Fact]
    public void RegisterFederation_ValidIdentity_ReturnsFederation()
    {
        var identityId = RegisterIdentity();

        var federation = _engine.RegisterFederation("google-oauth", "user@gmail.com", identityId);

        Assert.NotEqual(Guid.Empty, federation.FederationId);
        Assert.Equal("google-oauth", federation.Provider);
        Assert.Equal("user@gmail.com", federation.ExternalIdentityId);
        Assert.Equal(identityId, federation.InternalIdentityId);
        Assert.False(federation.Revoked);
    }

    [Fact]
    public void RegisterFederation_MissingIdentity_Throws()
    {
        var missingId = Guid.NewGuid();

        var ex = Assert.Throws<InvalidOperationException>(
            () => _engine.RegisterFederation("google-oauth", "user@gmail.com", missingId));

        Assert.Contains("does not exist", ex.Message);
    }

    [Fact]
    public void RegisterFederation_EmptyProvider_Throws()
    {
        var identityId = RegisterIdentity();

        Assert.Throws<ArgumentException>(
            () => _engine.RegisterFederation("", "user@gmail.com", identityId));
    }

    [Fact]
    public void RegisterFederation_EmptyExternalId_Throws()
    {
        var identityId = RegisterIdentity();

        Assert.Throws<ArgumentException>(
            () => _engine.RegisterFederation("google-oauth", "", identityId));
    }

    [Fact]
    public void ValidateFederation_ExistingMapping_ReturnsTrue()
    {
        var identityId = RegisterIdentity();
        _engine.RegisterFederation("azure-ad", "sub-12345", identityId);

        var result = _engine.ValidateFederation("azure-ad", "sub-12345");

        Assert.True(result);
    }

    [Fact]
    public void ValidateFederation_NoMapping_ReturnsFalse()
    {
        var result = _engine.ValidateFederation("azure-ad", "unknown-sub");

        Assert.False(result);
    }

    [Fact]
    public void RevokeFederation_ValidatedAfterRevoke_ReturnsFalse()
    {
        var identityId = RegisterIdentity();
        var federation = _engine.RegisterFederation("enterprise-sso", "emp-001", identityId);

        _engine.RevokeFederation(federation.FederationId);

        var result = _engine.ValidateFederation("enterprise-sso", "emp-001");
        Assert.False(result);
    }

    [Fact]
    public void GetFederations_ReturnsAllMappings()
    {
        var id1 = RegisterIdentity();
        var id2 = RegisterIdentity();

        _engine.RegisterFederation("google-oauth", "user1@gmail.com", id1);
        _engine.RegisterFederation("azure-ad", "sub-abc", id2);

        var federations = _engine.GetFederations();

        Assert.Equal(2, federations.Count);
    }
}
