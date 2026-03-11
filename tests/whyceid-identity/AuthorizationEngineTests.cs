using Whycespace.Engines.T0U.WhyceID;
using Whycespace.System.WhyceID.Aggregates;
using Whycespace.System.WhyceID.Models;
using Whycespace.System.WhyceID.Registry;
using Whycespace.System.WhyceID.Stores;

namespace Whycespace.WhyceID.Identity.Tests;

public class AuthorizationEngineTests
{
    private readonly IdentityRegistry _registry;
    private readonly IdentityRoleStore _roleStore;
    private readonly IdentityPermissionStore _permissionStore;
    private readonly IdentityAccessScopeStore _scopeStore;
    private readonly IdentityRoleEngine _roleEngine;
    private readonly IdentityPermissionEngine _permissionEngine;
    private readonly IdentityAccessScopeEngine _scopeEngine;
    private readonly AuthorizationEngine _engine;

    public AuthorizationEngineTests()
    {
        _registry = new IdentityRegistry();
        _roleStore = new IdentityRoleStore();
        _permissionStore = new IdentityPermissionStore();
        _scopeStore = new IdentityAccessScopeStore();
        _roleEngine = new IdentityRoleEngine(_registry, _roleStore);
        _permissionEngine = new IdentityPermissionEngine(_permissionStore);
        _scopeEngine = new IdentityAccessScopeEngine(_scopeStore);
        _engine = new AuthorizationEngine(_registry, _roleEngine, _permissionEngine, _scopeEngine);
    }

    private Guid RegisterIdentity()
    {
        var id = IdentityId.New();
        var identity = new IdentityAggregate(id, IdentityType.User);
        _registry.Register(identity);
        return id.Value;
    }

    private Guid SetupAuthorizedIdentity()
    {
        var identityId = RegisterIdentity();
        _roleEngine.AssignRole(identityId, "Admin");
        _permissionEngine.AssignPermission("Admin", "cluster:update");
        _scopeEngine.AssignScope("Admin", "cluster:whyceproperty");
        return identityId;
    }

    [Fact]
    public void Authorize_ShouldAllow_WhenRolePermissionAndScopeMatch()
    {
        var identityId = SetupAuthorizedIdentity();

        var result = _engine.Authorize(new AuthorizationRequest(
            identityId, "cluster", "update", "cluster:whyceproperty"));

        Assert.True(result.Allowed);
        Assert.Equal("Authorized", result.Reason);
    }

    [Fact]
    public void Authorize_MissingIdentity_ShouldDeny()
    {
        var result = _engine.Authorize(new AuthorizationRequest(
            Guid.NewGuid(), "cluster", "update", "cluster:whyceproperty"));

        Assert.False(result.Allowed);
        Assert.Equal("Identity does not exist", result.Reason);
    }

    [Fact]
    public void Authorize_MissingPermission_ShouldDeny()
    {
        var identityId = RegisterIdentity();
        _roleEngine.AssignRole(identityId, "Viewer");
        _scopeEngine.AssignScope("Viewer", "cluster:whyceproperty");

        var result = _engine.Authorize(new AuthorizationRequest(
            identityId, "cluster", "delete", "cluster:whyceproperty"));

        Assert.False(result.Allowed);
        Assert.Equal("Permission denied", result.Reason);
    }

    [Fact]
    public void Authorize_MissingScope_ShouldDeny()
    {
        var identityId = RegisterIdentity();
        _roleEngine.AssignRole(identityId, "Admin");
        _permissionEngine.AssignPermission("Admin", "cluster:update");

        var result = _engine.Authorize(new AuthorizationRequest(
            identityId, "cluster", "update", "cluster:whycemobility"));

        Assert.False(result.Allowed);
        Assert.Equal("Permission denied", result.Reason);
    }

    [Fact]
    public void Authorize_MultipleRoles_ShouldEvaluateAll()
    {
        var identityId = RegisterIdentity();
        _roleEngine.AssignRole(identityId, "Viewer");
        _roleEngine.AssignRole(identityId, "Operator");
        _permissionEngine.AssignPermission("Operator", "spv:create");
        _scopeEngine.AssignScope("Operator", "spv:taxi");

        var result = _engine.Authorize(new AuthorizationRequest(
            identityId, "spv", "create", "spv:taxi"));

        Assert.True(result.Allowed);
    }

    [Fact]
    public void Authorize_CorrectRole_ShouldSucceed()
    {
        var identityId = RegisterIdentity();
        _roleEngine.AssignRole(identityId, "Guardian");
        _permissionEngine.AssignPermission("Guardian", "vault:withdraw");
        _scopeEngine.AssignScope("Guardian", "system:global");

        var result = _engine.Authorize(new AuthorizationRequest(
            identityId, "vault", "withdraw", "system:global"));

        Assert.True(result.Allowed);
        Assert.Equal("Authorized", result.Reason);
    }
}
