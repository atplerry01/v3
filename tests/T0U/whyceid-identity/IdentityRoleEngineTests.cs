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

public class IdentityRoleEngineTests
{
    private readonly IdentityRegistry _registry;
    private readonly IdentityRoleStore _store;
    private readonly IdentityRoleEngine _engine;

    public IdentityRoleEngineTests()
    {
        _registry = new IdentityRegistry();
        _store = new IdentityRoleStore();
        _engine = new IdentityRoleEngine(_registry, _store);
    }

    private Guid RegisterIdentity()
    {
        var id = IdentityId.New();
        var identity = new IdentityAggregate(id, IdentityType.User);
        _registry.Register(identity);
        return id.Value;
    }

    [Fact]
    public void AssignRole_ShouldSucceed()
    {
        var identityId = RegisterIdentity();

        _engine.AssignRole(identityId, "Admin");

        var roles = _engine.GetRoles(identityId);
        Assert.Single(roles);
        Assert.Contains("Admin", roles);
    }

    [Fact]
    public void AssignRole_Duplicate_ShouldBeIgnored()
    {
        var identityId = RegisterIdentity();

        _engine.AssignRole(identityId, "Operator");
        _engine.AssignRole(identityId, "Operator");

        var roles = _engine.GetRoles(identityId);
        Assert.Single(roles);
    }

    [Fact]
    public void AssignRole_IdentityNotFound_ShouldThrow()
    {
        Assert.Throws<InvalidOperationException>(() =>
            _engine.AssignRole(Guid.NewGuid(), "Admin"));
    }

    [Fact]
    public void AssignRole_EmptyRole_ShouldThrow()
    {
        var identityId = RegisterIdentity();

        Assert.Throws<ArgumentException>(() =>
            _engine.AssignRole(identityId, ""));
    }

    [Fact]
    public void AssignRole_WhitespaceRole_ShouldThrow()
    {
        var identityId = RegisterIdentity();

        Assert.Throws<ArgumentException>(() =>
            _engine.AssignRole(identityId, "  "));
    }

    [Fact]
    public void AssignMultipleRoles_ShouldBeRetrievable()
    {
        var identityId = RegisterIdentity();

        _engine.AssignRole(identityId, "Admin");
        _engine.AssignRole(identityId, "Operator");
        _engine.AssignRole(identityId, "Developer");

        var roles = _engine.GetRoles(identityId);
        Assert.Equal(3, roles.Count);
        Assert.Contains("Admin", roles);
        Assert.Contains("Operator", roles);
        Assert.Contains("Developer", roles);
    }

    [Fact]
    public void GetRoles_UnknownIdentity_ShouldReturnEmpty()
    {
        var roles = _engine.GetRoles(Guid.NewGuid());

        Assert.Empty(roles);
    }

    [Fact]
    public void HasRole_ShouldReturnTrue_WhenAssigned()
    {
        var identityId = RegisterIdentity();

        _engine.AssignRole(identityId, "Guardian");

        Assert.True(_engine.HasRole(identityId, "Guardian"));
    }

    [Fact]
    public void HasRole_ShouldReturnFalse_WhenNotAssigned()
    {
        var identityId = RegisterIdentity();

        Assert.False(_engine.HasRole(identityId, "Admin"));
    }

    [Fact]
    public void HasRole_ShouldReturnFalse_ForUnknownIdentity()
    {
        Assert.False(_engine.HasRole(Guid.NewGuid(), "Admin"));
    }
}
