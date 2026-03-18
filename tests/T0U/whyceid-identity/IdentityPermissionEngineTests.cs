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
using Whycespace.Systems.WhyceID.Stores;

namespace Whycespace.WhyceID.Identity.Tests;

public class IdentityPermissionEngineTests
{
    private readonly IdentityPermissionStore _store;
    private readonly IdentityPermissionEngine _engine;

    public IdentityPermissionEngineTests()
    {
        _store = new IdentityPermissionStore();
        _engine = new IdentityPermissionEngine(_store);
    }

    [Fact]
    public void AssignPermission_ShouldSucceed()
    {
        _engine.AssignPermission("Admin", "cluster:create");

        var permissions = _engine.GetPermissions("Admin");
        Assert.Single(permissions);
        Assert.Contains("cluster:create", permissions);
    }

    [Fact]
    public void AssignPermission_Duplicate_ShouldBeIgnored()
    {
        _engine.AssignPermission("Admin", "cluster:create");
        _engine.AssignPermission("Admin", "cluster:create");

        var permissions = _engine.GetPermissions("Admin");
        Assert.Single(permissions);
    }

    [Fact]
    public void AssignPermission_EmptyRole_ShouldThrow()
    {
        Assert.Throws<ArgumentException>(() =>
            _engine.AssignPermission("", "cluster:create"));
    }

    [Fact]
    public void AssignPermission_WhitespaceRole_ShouldThrow()
    {
        Assert.Throws<ArgumentException>(() =>
            _engine.AssignPermission("  ", "cluster:create"));
    }

    [Fact]
    public void AssignPermission_EmptyPermission_ShouldThrow()
    {
        Assert.Throws<ArgumentException>(() =>
            _engine.AssignPermission("Admin", ""));
    }

    [Fact]
    public void AssignPermission_InvalidFormat_ShouldThrow()
    {
        Assert.Throws<ArgumentException>(() =>
            _engine.AssignPermission("Admin", "invalidpermission"));
    }

    [Fact]
    public void GetPermissions_UnknownRole_ShouldReturnEmpty()
    {
        var permissions = _engine.GetPermissions("NonExistent");

        Assert.Empty(permissions);
    }

    [Fact]
    public void AssignMultiplePermissions_ShouldBeRetrievable()
    {
        _engine.AssignPermission("Admin", "cluster:create");
        _engine.AssignPermission("Admin", "cluster:delete");
        _engine.AssignPermission("Admin", "spv:manage");

        var permissions = _engine.GetPermissions("Admin");
        Assert.Equal(3, permissions.Count);
        Assert.Contains("cluster:create", permissions);
        Assert.Contains("cluster:delete", permissions);
        Assert.Contains("spv:manage", permissions);
    }

    [Fact]
    public void HasPermission_ShouldReturnTrue_WhenAssigned()
    {
        _engine.AssignPermission("Operator", "system:operate");

        Assert.True(_engine.HasPermission("Operator", "system:operate"));
    }

    [Fact]
    public void HasPermission_ShouldReturnFalse_WhenNotAssigned()
    {
        Assert.False(_engine.HasPermission("Operator", "cluster:create"));
    }

    [Fact]
    public void HasPermission_ShouldReturnFalse_ForUnknownRole()
    {
        Assert.False(_engine.HasPermission("NonExistent", "cluster:create"));
    }
}
