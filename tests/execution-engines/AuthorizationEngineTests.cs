namespace Whycespace.ExecutionEngines.Tests;

using Whycespace.Engines.T2E.Core.Identity;
using Whycespace.Contracts.Engines;

public sealed class AuthorizationEngineTests
{
    private readonly AuthorizationEngine _engine = new();

    private static EngineContext CreateContext(Dictionary<string, object> data)
    {
        return new EngineContext(
            Guid.NewGuid(), Guid.NewGuid().ToString(), "Authorize",
            "partition-1", data);
    }

    private static Dictionary<string, object> ValidData() => new()
    {
        ["identityId"] = Guid.NewGuid().ToString(),
        ["resourceType"] = "vault",
        ["resourceId"] = Guid.NewGuid().ToString(),
        ["action"] = "read",
        ["requiredPermission"] = "vault:read",
        ["accessScope"] = "global",
        ["authenticated"] = true,
        ["trustScore"] = 0.8,
        ["deviceTrustScore"] = 0.7,
        ["permissions"] = "vault:read,vault:write,resource:read"
    };

    [Fact]
    public async Task AuthorizeValidPermission_ShouldSucceed()
    {
        var data = ValidData();
        var context = CreateContext(data);

        var result = await _engine.ExecuteAsync(context);

        Assert.True(result.Success);
        Assert.Single(result.Events);
        Assert.Equal("AuthorizationEvaluated", result.Events[0].EventType);
        Assert.Equal(true, result.Output["authorized"]);
        Assert.Equal("vault:read", result.Output["permissionGranted"]);
        Assert.Equal(true, result.Output["scopeValidated"]);
    }

    [Fact]
    public async Task DenyMissingPermission_ShouldDeny()
    {
        var data = ValidData();
        data["requiredPermission"] = "admin:delete";
        var context = CreateContext(data);

        var result = await _engine.ExecuteAsync(context);

        Assert.True(result.Success);
        Assert.Equal(false, result.Output["authorized"]);
        Assert.Contains("Missing required permission", result.Output["reason"] as string);
    }

    [Fact]
    public async Task DenyInvalidScope_ShouldDeny()
    {
        var data = ValidData();
        data["accessScope"] = "invalid_scope";
        var context = CreateContext(data);

        var result = await _engine.ExecuteAsync(context);

        Assert.True(result.Success);
        Assert.Equal(false, result.Output["authorized"]);
        Assert.Contains("Invalid access scope", result.Output["reason"] as string);
    }

    [Fact]
    public async Task DenyLowTrustScore_ShouldDeny()
    {
        var data = ValidData();
        data["trustScore"] = 0.2;
        var context = CreateContext(data);

        var result = await _engine.ExecuteAsync(context);

        Assert.True(result.Success);
        Assert.Equal(false, result.Output["authorized"]);
        Assert.Contains("Trust score", result.Output["reason"] as string);
    }

    [Fact]
    public async Task DenyLowDeviceTrustScore_ShouldDeny()
    {
        var data = ValidData();
        data["deviceTrustScore"] = 0.1;
        var context = CreateContext(data);

        var result = await _engine.ExecuteAsync(context);

        Assert.True(result.Success);
        Assert.Equal(false, result.Output["authorized"]);
        Assert.Contains("Device trust score", result.Output["reason"] as string);
    }

    [Fact]
    public async Task PolicyDenial_ShouldDeny()
    {
        var data = ValidData();
        data["policyDenied"] = true;
        var context = CreateContext(data);

        var result = await _engine.ExecuteAsync(context);

        Assert.True(result.Success);
        Assert.Equal(false, result.Output["authorized"]);
        Assert.Contains("denied by policy", result.Output["reason"] as string);
    }

    [Fact]
    public async Task UnauthenticatedIdentity_ShouldDeny()
    {
        var data = ValidData();
        data["authenticated"] = false;
        var context = CreateContext(data);

        var result = await _engine.ExecuteAsync(context);

        Assert.True(result.Success);
        Assert.Equal(false, result.Output["authorized"]);
        Assert.Contains("not authenticated", result.Output["reason"] as string);
    }

    [Fact]
    public async Task MissingIdentityId_ShouldFail()
    {
        var data = ValidData();
        data.Remove("identityId");
        var context = CreateContext(data);

        var result = await _engine.ExecuteAsync(context);

        Assert.False(result.Success);
    }

    [Fact]
    public async Task MissingRequiredPermission_ShouldFail()
    {
        var data = ValidData();
        data.Remove("requiredPermission");
        var context = CreateContext(data);

        var result = await _engine.ExecuteAsync(context);

        Assert.False(result.Success);
    }

    [Fact]
    public async Task NoPermissionsAssigned_ShouldDeny()
    {
        var data = ValidData();
        data.Remove("permissions");
        var context = CreateContext(data);

        var result = await _engine.ExecuteAsync(context);

        Assert.True(result.Success);
        Assert.Equal(false, result.Output["authorized"]);
        Assert.Contains("No permissions assigned", result.Output["reason"] as string);
    }

    [Fact]
    public async Task EventPayload_ShouldContainCorrectFields()
    {
        var data = ValidData();
        var identityId = data["identityId"] as string;
        var context = CreateContext(data);

        var result = await _engine.ExecuteAsync(context);

        var payload = result.Events[0].Payload;
        Assert.Equal(identityId, payload["identityId"]);
        Assert.Equal("vault", payload["resourceType"]);
        Assert.Equal("read", payload["action"]);
        Assert.Equal(true, payload["authorized"]);
        Assert.Equal(1, payload["eventVersion"]);
        Assert.Equal("whyce.identity.events", payload["topic"]);
    }

    [Fact]
    public async Task ConcurrentAuthorizations_ShouldAllSucceed()
    {
        var tasks = new Task<EngineResult>[100];

        for (int i = 0; i < tasks.Length; i++)
        {
            var index = i;
            tasks[i] = Task.Run(async () =>
            {
                var data = ValidData();
                data["identityId"] = Guid.NewGuid().ToString();
                data["requiredPermission"] = "vault:read";
                var context = CreateContext(data);
                return await _engine.ExecuteAsync(context);
            });
        }

        var results = await Task.WhenAll(tasks);

        Assert.All(results, r =>
        {
            Assert.True(r.Success);
            Assert.Equal(true, r.Output["authorized"]);
        });
    }
}
