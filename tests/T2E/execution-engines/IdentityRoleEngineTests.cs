namespace Whycespace.ExecutionEngines.Tests;

using Whycespace.Engines.T2E.Identity.Engines;
using Whycespace.Engines.T2E.Identity.Models;
using Whycespace.Contracts.Engines;

public sealed class IdentityRoleEngineTests
{
    private readonly IdentityRoleEngine _engine = new();

    private static EngineContext CreateAssignContext(
        string? identityId = null,
        string? roleId = null,
        string? roleName = null,
        string? grantedBy = null)
    {
        var data = new Dictionary<string, object> { ["operation"] = "assign" };

        if (identityId is not null) data["identityId"] = identityId;
        if (roleId is not null) data["roleId"] = roleId;
        if (roleName is not null) data["roleName"] = roleName;
        if (grantedBy is not null) data["grantedBy"] = grantedBy;

        return new EngineContext(
            Guid.NewGuid(), Guid.NewGuid().ToString(), "IdentityRoleMutation",
            "partition-1", data);
    }

    private static EngineContext CreateRevokeContext(
        string? identityId = null,
        string? roleId = null,
        string? revokedBy = null)
    {
        var data = new Dictionary<string, object> { ["operation"] = "revoke" };

        if (identityId is not null) data["identityId"] = identityId;
        if (roleId is not null) data["roleId"] = roleId;
        if (revokedBy is not null) data["revokedBy"] = revokedBy;

        return new EngineContext(
            Guid.NewGuid(), Guid.NewGuid().ToString(), "IdentityRoleMutation",
            "partition-1", data);
    }

    // --- Assign Role Tests ---

    [Fact]
    public async Task AssignRole_ShouldSucceed_WithValidInput()
    {
        var identityId = Guid.NewGuid().ToString();
        var grantedBy = Guid.NewGuid().ToString();
        var context = CreateAssignContext(identityId, "role-admin", "Admin", grantedBy);

        var result = await _engine.ExecuteAsync(context);

        Assert.True(result.Success);
        Assert.Single(result.Events);
        Assert.Equal("IdentityRoleAssigned", result.Events[0].EventType);
        Assert.Equal(identityId, result.Output["identityId"]);
        Assert.Equal("role-admin", result.Output["roleId"]);
        Assert.Equal("Admin", result.Output["roleName"]);
        Assert.Equal("Assigned", result.Output["mutationType"]);
        Assert.Equal(grantedBy, result.Output["executedBy"]);
    }

    [Fact]
    public async Task AssignRole_ShouldEmitEvent_WithCorrectPayload()
    {
        var identityId = Guid.NewGuid().ToString();
        var grantedBy = Guid.NewGuid().ToString();
        var context = CreateAssignContext(identityId, "role-ops", "Operator", grantedBy);

        var result = await _engine.ExecuteAsync(context);

        var evt = result.Events[0];
        Assert.Equal("IdentityRoleAssigned", evt.EventType);
        Assert.Equal(identityId, evt.Payload["identityId"]);
        Assert.Equal("role-ops", evt.Payload["roleId"]);
        Assert.Equal("Operator", evt.Payload["roleName"]);
        Assert.Equal(grantedBy, evt.Payload["grantedBy"]);
        Assert.Equal(1, evt.Payload["eventVersion"]);
        Assert.Equal("whyce.identity.events", evt.Payload["topic"]);
    }

    [Fact]
    public async Task AssignRole_MissingIdentityId_ShouldFail()
    {
        var context = CreateAssignContext(
            roleId: "role-1", roleName: "Admin", grantedBy: Guid.NewGuid().ToString());

        var result = await _engine.ExecuteAsync(context);

        Assert.False(result.Success);
    }

    [Fact]
    public async Task AssignRole_EmptyIdentityId_ShouldFail()
    {
        var context = CreateAssignContext(
            Guid.Empty.ToString(), "role-1", "Admin", Guid.NewGuid().ToString());

        var result = await _engine.ExecuteAsync(context);

        Assert.False(result.Success);
    }

    [Fact]
    public async Task AssignRole_MissingRoleId_ShouldFail()
    {
        var context = CreateAssignContext(
            identityId: Guid.NewGuid().ToString(), roleName: "Admin",
            grantedBy: Guid.NewGuid().ToString());

        var result = await _engine.ExecuteAsync(context);

        Assert.False(result.Success);
    }

    [Fact]
    public async Task AssignRole_MissingRoleName_ShouldFail()
    {
        var context = CreateAssignContext(
            identityId: Guid.NewGuid().ToString(), roleId: "role-1",
            grantedBy: Guid.NewGuid().ToString());

        var result = await _engine.ExecuteAsync(context);

        Assert.False(result.Success);
    }

    [Fact]
    public async Task AssignRole_MissingGrantedBy_ShouldFail()
    {
        var context = CreateAssignContext(
            identityId: Guid.NewGuid().ToString(), roleId: "role-1", roleName: "Admin");

        var result = await _engine.ExecuteAsync(context);

        Assert.False(result.Success);
    }

    // --- Revoke Role Tests ---

    [Fact]
    public async Task RevokeRole_ShouldSucceed_WithValidInput()
    {
        var identityId = Guid.NewGuid().ToString();
        var revokedBy = Guid.NewGuid().ToString();
        var context = CreateRevokeContext(identityId, "role-admin", revokedBy);

        var result = await _engine.ExecuteAsync(context);

        Assert.True(result.Success);
        Assert.Single(result.Events);
        Assert.Equal("IdentityRoleRevoked", result.Events[0].EventType);
        Assert.Equal(identityId, result.Output["identityId"]);
        Assert.Equal("role-admin", result.Output["roleId"]);
        Assert.Equal("Revoked", result.Output["mutationType"]);
        Assert.Equal(revokedBy, result.Output["executedBy"]);
    }

    [Fact]
    public async Task RevokeRole_ShouldEmitEvent_WithCorrectPayload()
    {
        var identityId = Guid.NewGuid().ToString();
        var revokedBy = Guid.NewGuid().ToString();
        var context = CreateRevokeContext(identityId, "role-ops", revokedBy);

        var result = await _engine.ExecuteAsync(context);

        var evt = result.Events[0];
        Assert.Equal("IdentityRoleRevoked", evt.EventType);
        Assert.Equal(identityId, evt.Payload["identityId"]);
        Assert.Equal("role-ops", evt.Payload["roleId"]);
        Assert.Equal(revokedBy, evt.Payload["revokedBy"]);
        Assert.Equal(1, evt.Payload["eventVersion"]);
        Assert.Equal("whyce.identity.events", evt.Payload["topic"]);
    }

    [Fact]
    public async Task RevokeRole_MissingIdentityId_ShouldFail()
    {
        var context = CreateRevokeContext(
            roleId: "role-1", revokedBy: Guid.NewGuid().ToString());

        var result = await _engine.ExecuteAsync(context);

        Assert.False(result.Success);
    }

    [Fact]
    public async Task RevokeRole_MissingRoleId_ShouldFail()
    {
        var context = CreateRevokeContext(
            identityId: Guid.NewGuid().ToString(), revokedBy: Guid.NewGuid().ToString());

        var result = await _engine.ExecuteAsync(context);

        Assert.False(result.Success);
    }

    [Fact]
    public async Task RevokeRole_MissingRevokedBy_ShouldFail()
    {
        var context = CreateRevokeContext(
            identityId: Guid.NewGuid().ToString(), roleId: "role-1");

        var result = await _engine.ExecuteAsync(context);

        Assert.False(result.Success);
    }

    // --- General Tests ---

    [Fact]
    public async Task MissingOperation_ShouldFail()
    {
        var context = new EngineContext(
            Guid.NewGuid(), Guid.NewGuid().ToString(), "IdentityRoleMutation",
            "partition-1", new Dictionary<string, object>());

        var result = await _engine.ExecuteAsync(context);

        Assert.False(result.Success);
    }

    [Fact]
    public async Task UnknownOperation_ShouldFail()
    {
        var context = new EngineContext(
            Guid.NewGuid(), Guid.NewGuid().ToString(), "IdentityRoleMutation",
            "partition-1", new Dictionary<string, object> { ["operation"] = "delete" });

        var result = await _engine.ExecuteAsync(context);

        Assert.False(result.Success);
    }

    // --- Concurrency Tests ---

    [Fact]
    public async Task ParallelRoleAssignments_ShouldAllSucceed()
    {
        var tasks = Enumerable.Range(0, 100).Select(i =>
        {
            var context = CreateAssignContext(
                Guid.NewGuid().ToString(),
                $"role-{i}",
                $"Role {i}",
                Guid.NewGuid().ToString());
            return _engine.ExecuteAsync(context);
        }).ToArray();

        var results = await Task.WhenAll(tasks);

        Assert.All(results, r => Assert.True(r.Success));
        Assert.Equal(100, results.Length);
    }
}
