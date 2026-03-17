namespace Whycespace.ExecutionEngines.Tests;

using Whycespace.Engines.T2E.Identity.Engines;
using Whycespace.Engines.T2E.Identity.Models;
using Whycespace.Contracts.Engines;

public sealed class IdentityAccessScopeEngineTests
{
    private readonly IdentityAccessScopeEngine _engine = new();

    private EngineContext CreateContext(string operation, Dictionary<string, object> data)
    {
        data["operation"] = operation;
        return new EngineContext(
            Guid.NewGuid(), Guid.NewGuid().ToString(), "ScopeMutation",
            "partition-1", data);
    }

    [Fact]
    public async Task GrantScope_ShouldSucceed()
    {
        var identityId = Guid.NewGuid().ToString();
        var grantedBy = Guid.NewGuid().ToString();
        var context = CreateContext("grant", new Dictionary<string, object>
        {
            ["identityId"] = identityId,
            ["scopeKey"] = "cluster:mobility",
            ["grantedBy"] = grantedBy
        });

        var result = await _engine.ExecuteAsync(context);

        Assert.True(result.Success);
        Assert.Single(result.Events);
        Assert.Equal("IdentityScopeGranted", result.Events[0].EventType);
        Assert.Equal(identityId, result.Output["identityId"]);
        Assert.Equal("cluster:mobility", result.Output["scopeKey"]);
        Assert.Equal("Granted", result.Output["mutationType"]);
    }

    [Fact]
    public async Task GrantScope_MissingIdentityId_ShouldFail()
    {
        var context = CreateContext("grant", new Dictionary<string, object>
        {
            ["scopeKey"] = "cluster:mobility",
            ["grantedBy"] = Guid.NewGuid().ToString()
        });

        var result = await _engine.ExecuteAsync(context);

        Assert.False(result.Success);
    }

    [Fact]
    public async Task GrantScope_InvalidIdentityId_ShouldFail()
    {
        var context = CreateContext("grant", new Dictionary<string, object>
        {
            ["identityId"] = Guid.Empty.ToString(),
            ["scopeKey"] = "cluster:mobility",
            ["grantedBy"] = Guid.NewGuid().ToString()
        });

        var result = await _engine.ExecuteAsync(context);

        Assert.False(result.Success);
    }

    [Fact]
    public async Task GrantScope_MissingScopeKey_ShouldFail()
    {
        var context = CreateContext("grant", new Dictionary<string, object>
        {
            ["identityId"] = Guid.NewGuid().ToString(),
            ["grantedBy"] = Guid.NewGuid().ToString()
        });

        var result = await _engine.ExecuteAsync(context);

        Assert.False(result.Success);
    }

    [Theory]
    [InlineData("invalid-scope")]
    [InlineData("unknown:value")]
    [InlineData("cluster:")]
    [InlineData(":mobility")]
    [InlineData("")]
    [InlineData("cluster:UPPERCASE")]
    [InlineData("cluster:has spaces")]
    public async Task GrantScope_InvalidScopeFormat_ShouldFail(string scopeKey)
    {
        var context = CreateContext("grant", new Dictionary<string, object>
        {
            ["identityId"] = Guid.NewGuid().ToString(),
            ["scopeKey"] = scopeKey,
            ["grantedBy"] = Guid.NewGuid().ToString()
        });

        var result = await _engine.ExecuteAsync(context);

        Assert.False(result.Success);
    }

    [Theory]
    [InlineData("cluster:mobility")]
    [InlineData("cluster:property")]
    [InlineData("spv:property-acquisition")]
    [InlineData("domain:governance")]
    [InlineData("system:whycepolicy")]
    public async Task GrantScope_ValidScopeFormats_ShouldSucceed(string scopeKey)
    {
        var context = CreateContext("grant", new Dictionary<string, object>
        {
            ["identityId"] = Guid.NewGuid().ToString(),
            ["scopeKey"] = scopeKey,
            ["grantedBy"] = Guid.NewGuid().ToString()
        });

        var result = await _engine.ExecuteAsync(context);

        Assert.True(result.Success);
        Assert.Equal(scopeKey, result.Output["scopeKey"]);
    }

    [Fact]
    public async Task RevokeScope_ShouldSucceed()
    {
        var identityId = Guid.NewGuid().ToString();
        var revokedBy = Guid.NewGuid().ToString();
        var context = CreateContext("revoke", new Dictionary<string, object>
        {
            ["identityId"] = identityId,
            ["scopeKey"] = "spv:property-acquisition",
            ["revokedBy"] = revokedBy
        });

        var result = await _engine.ExecuteAsync(context);

        Assert.True(result.Success);
        Assert.Single(result.Events);
        Assert.Equal("IdentityScopeRevoked", result.Events[0].EventType);
        Assert.Equal(identityId, result.Output["identityId"]);
        Assert.Equal("spv:property-acquisition", result.Output["scopeKey"]);
        Assert.Equal("Revoked", result.Output["mutationType"]);
    }

    [Fact]
    public async Task RevokeScope_MissingScopeKey_ShouldFail()
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
    public async Task RevokeScope_InvalidScopeFormat_ShouldFail()
    {
        var context = CreateContext("revoke", new Dictionary<string, object>
        {
            ["identityId"] = Guid.NewGuid().ToString(),
            ["scopeKey"] = "bad-format",
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
            Guid.NewGuid(), Guid.NewGuid().ToString(), "ScopeMutation",
            "partition-1", new Dictionary<string, object>());

        var result = await _engine.ExecuteAsync(context);

        Assert.False(result.Success);
    }

    [Fact]
    public async Task GrantScope_EventPayload_ShouldContainCorrectFields()
    {
        var identityId = Guid.NewGuid().ToString();
        var grantedBy = Guid.NewGuid().ToString();
        var context = CreateContext("grant", new Dictionary<string, object>
        {
            ["identityId"] = identityId,
            ["scopeKey"] = "domain:governance",
            ["grantedBy"] = grantedBy
        });

        var result = await _engine.ExecuteAsync(context);

        var eventPayload = result.Events[0].Payload;
        Assert.Equal(identityId, eventPayload["identityId"]);
        Assert.Equal("domain:governance", eventPayload["scopeKey"]);
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
                    ["scopeKey"] = $"cluster:scope-{index}",
                    ["grantedBy"] = Guid.NewGuid().ToString()
                });
                return await _engine.ExecuteAsync(context);
            });
        }

        var results = await Task.WhenAll(tasks);

        Assert.All(results, r => Assert.True(r.Success));
    }
}
