namespace Whycespace.ExecutionEngines.Tests;

using Whycespace.Engines.T2E.System.Identity;
using Whycespace.Contracts.Engines;

public sealed class IdentityPermissionEngineTests
{
    private readonly IdentityPermissionEngine _engine = new();

    private EngineContext CreateContext(string operation, Dictionary<string, object> data)
    {
        data["operation"] = operation;
        return new EngineContext(
            Guid.NewGuid(), Guid.NewGuid().ToString(), "PermissionMutation",
            "partition-1", data);
    }

    [Fact]
    public async Task GrantPermission_ShouldSucceed()
    {
        var identityId = Guid.NewGuid().ToString();
        var grantedBy = Guid.NewGuid().ToString();
        var context = CreateContext("grant", new Dictionary<string, object>
        {
            ["identityId"] = identityId,
            ["permissionKey"] = "resource:read",
            ["grantedBy"] = grantedBy
        });

        var result = await _engine.ExecuteAsync(context);

        Assert.True(result.Success);
        Assert.Single(result.Events);
        Assert.Equal("IdentityPermissionGranted", result.Events[0].EventType);
        Assert.Equal(identityId, result.Output["identityId"]);
        Assert.Equal("resource:read", result.Output["permissionKey"]);
        Assert.Equal("Granted", result.Output["mutationType"]);
    }

    [Fact]
    public async Task GrantPermission_MissingIdentityId_ShouldFail()
    {
        var context = CreateContext("grant", new Dictionary<string, object>
        {
            ["permissionKey"] = "resource:read",
            ["grantedBy"] = Guid.NewGuid().ToString()
        });

        var result = await _engine.ExecuteAsync(context);

        Assert.False(result.Success);
    }

    [Fact]
    public async Task GrantPermission_InvalidIdentityId_ShouldFail()
    {
        var context = CreateContext("grant", new Dictionary<string, object>
        {
            ["identityId"] = Guid.Empty.ToString(),
            ["permissionKey"] = "resource:read",
            ["grantedBy"] = Guid.NewGuid().ToString()
        });

        var result = await _engine.ExecuteAsync(context);

        Assert.False(result.Success);
    }

    [Fact]
    public async Task GrantPermission_MissingPermissionKey_ShouldFail()
    {
        var context = CreateContext("grant", new Dictionary<string, object>
        {
            ["identityId"] = Guid.NewGuid().ToString(),
            ["grantedBy"] = Guid.NewGuid().ToString()
        });

        var result = await _engine.ExecuteAsync(context);

        Assert.False(result.Success);
    }

    [Fact]
    public async Task RevokePermission_ShouldSucceed()
    {
        var identityId = Guid.NewGuid().ToString();
        var revokedBy = Guid.NewGuid().ToString();
        var context = CreateContext("revoke", new Dictionary<string, object>
        {
            ["identityId"] = identityId,
            ["permissionKey"] = "resource:write",
            ["revokedBy"] = revokedBy
        });

        var result = await _engine.ExecuteAsync(context);

        Assert.True(result.Success);
        Assert.Single(result.Events);
        Assert.Equal("IdentityPermissionRevoked", result.Events[0].EventType);
        Assert.Equal(identityId, result.Output["identityId"]);
        Assert.Equal("resource:write", result.Output["permissionKey"]);
        Assert.Equal("Revoked", result.Output["mutationType"]);
    }

    [Fact]
    public async Task RevokePermission_MissingPermissionKey_ShouldFail()
    {
        var context = CreateContext("revoke", new Dictionary<string, object>
        {
            ["identityId"] = Guid.NewGuid().ToString(),
            ["revokedBy"] = Guid.NewGuid().ToString()
        });

        var result = await _engine.ExecuteAsync(context);

        Assert.False(result.Success);
    }

    [Fact]
    public async Task UnknownOperation_ShouldFail()
    {
        var context = CreateContext("unknown", new Dictionary<string, object>
        {
            ["identityId"] = Guid.NewGuid().ToString()
        });

        var result = await _engine.ExecuteAsync(context);

        Assert.False(result.Success);
    }

    [Fact]
    public async Task MissingOperation_ShouldFail()
    {
        var context = new EngineContext(
            Guid.NewGuid(), Guid.NewGuid().ToString(), "PermissionMutation",
            "partition-1", new Dictionary<string, object>());

        var result = await _engine.ExecuteAsync(context);

        Assert.False(result.Success);
    }

    [Fact]
    public async Task GrantPermission_EventPayload_ShouldContainCorrectFields()
    {
        var identityId = Guid.NewGuid().ToString();
        var grantedBy = Guid.NewGuid().ToString();
        var context = CreateContext("grant", new Dictionary<string, object>
        {
            ["identityId"] = identityId,
            ["permissionKey"] = "vault:create",
            ["grantedBy"] = grantedBy
        });

        var result = await _engine.ExecuteAsync(context);

        var eventPayload = result.Events[0].Payload;
        Assert.Equal(identityId, eventPayload["identityId"]);
        Assert.Equal("vault:create", eventPayload["permissionKey"]);
        Assert.Equal(grantedBy, eventPayload["grantedBy"]);
        Assert.Equal(1, eventPayload["eventVersion"]);
        Assert.Equal("whyce.identity.events", eventPayload["topic"]);
    }

    [Fact]
    public async Task ConcurrentGrants_ShouldAllSucceed()
    {
        var tasks = new Task<EngineResult>[100];

        for (int i = 0; i < tasks.Length; i++)
        {
            var index = i;
            tasks[i] = Task.Run(async () =>
            {
                var context = CreateContext("grant", new Dictionary<string, object>
                {
                    ["identityId"] = Guid.NewGuid().ToString(),
                    ["permissionKey"] = $"resource_{index}:read",
                    ["grantedBy"] = Guid.NewGuid().ToString()
                });
                return await _engine.ExecuteAsync(context);
            });
        }

        var results = await Task.WhenAll(tasks);

        Assert.All(results, r => Assert.True(r.Success));
    }
}
