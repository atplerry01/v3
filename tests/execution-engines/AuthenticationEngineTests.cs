namespace Whycespace.ExecutionEngines.Tests;

using Whycespace.Engines.T2E.Core.Identity;
using Whycespace.Contracts.Engines;

public sealed class AuthenticationEngineTests
{
    private readonly AuthenticationEngine _engine = new();

    private static EngineContext CreateContext(Dictionary<string, object> data)
    {
        return new EngineContext(
            Guid.NewGuid(), Guid.NewGuid().ToString(), "Authenticate",
            "partition-1", data);
    }

    private static Dictionary<string, object> ValidAuthData()
    {
        return new Dictionary<string, object>
        {
            ["identityId"] = Guid.NewGuid().ToString(),
            ["credentialType"] = "Password",
            ["credentialValue"] = "hashed-credential-value",
            ["deviceId"] = "device-001",
            ["deviceFingerprint"] = "fp-abc123",
            ["ipAddress"] = "192.168.1.1",
            ["geoLocation"] = "London, UK",
            ["authenticationMethod"] = "Password",
            ["identityExists"] = true,
            ["identityStatus"] = "Active",
            ["credentialValid"] = true,
            ["deviceTrustScore"] = 0.85
        };
    }

    [Fact]
    public async Task Authenticate_ValidIdentity_ShouldSucceed()
    {
        var data = ValidAuthData();
        var context = CreateContext(data);

        var result = await _engine.ExecuteAsync(context);

        Assert.True(result.Success);
        Assert.Single(result.Events);
        Assert.Equal("IdentityAuthenticated", result.Events[0].EventType);
        Assert.Equal(true, result.Output["authenticated"]);
        Assert.Equal(data["identityId"], result.Output["identityId"]);
        Assert.Equal("Password", result.Output["authenticationMethod"]);
    }

    [Fact]
    public async Task Authenticate_InvalidCredentials_ShouldFail()
    {
        var data = ValidAuthData();
        data["credentialValid"] = false;
        var context = CreateContext(data);

        var result = await _engine.ExecuteAsync(context);

        Assert.False(result.Success);
        Assert.Empty(result.Events);
        Assert.Equal(false, result.Output["authenticated"]);
        Assert.Equal("Invalid credentials", result.Output["failureReason"]);
    }

    [Fact]
    public async Task Authenticate_InactiveIdentity_ShouldFail()
    {
        var data = ValidAuthData();
        data["identityStatus"] = "Suspended";
        var context = CreateContext(data);

        var result = await _engine.ExecuteAsync(context);

        Assert.False(result.Success);
        Assert.Equal(false, result.Output["authenticated"]);
        Assert.Equal("Identity is not active", result.Output["failureReason"]);
    }

    [Fact]
    public async Task Authenticate_UntrustedDevice_ShouldFail()
    {
        var data = ValidAuthData();
        data["deviceTrustScore"] = 0.3;
        var context = CreateContext(data);

        var result = await _engine.ExecuteAsync(context);

        Assert.False(result.Success);
        Assert.Equal(false, result.Output["authenticated"]);
        Assert.Equal("Untrusted device", result.Output["failureReason"]);
    }

    [Fact]
    public async Task Authenticate_NonExistentIdentity_ShouldFail()
    {
        var data = ValidAuthData();
        data["identityExists"] = false;
        var context = CreateContext(data);

        var result = await _engine.ExecuteAsync(context);

        Assert.False(result.Success);
        Assert.Equal(false, result.Output["authenticated"]);
        Assert.Equal("Identity does not exist", result.Output["failureReason"]);
    }

    [Fact]
    public async Task Authenticate_MissingIdentityId_ShouldFail()
    {
        var data = ValidAuthData();
        data.Remove("identityId");
        var context = CreateContext(data);

        var result = await _engine.ExecuteAsync(context);

        Assert.False(result.Success);
    }

    [Fact]
    public async Task Authenticate_EmptyIdentityId_ShouldFail()
    {
        var data = ValidAuthData();
        data["identityId"] = Guid.Empty.ToString();
        var context = CreateContext(data);

        var result = await _engine.ExecuteAsync(context);

        Assert.False(result.Success);
    }

    [Fact]
    public async Task Authenticate_InvalidAuthenticationMethod_ShouldFail()
    {
        var data = ValidAuthData();
        data["authenticationMethod"] = "InvalidMethod";
        var context = CreateContext(data);

        var result = await _engine.ExecuteAsync(context);

        Assert.False(result.Success);
    }

    [Theory]
    [InlineData("Password")]
    [InlineData("Token")]
    [InlineData("OAuth")]
    [InlineData("APIKey")]
    public async Task Authenticate_AllValidMethods_ShouldSucceed(string method)
    {
        var data = ValidAuthData();
        data["authenticationMethod"] = method;
        var context = CreateContext(data);

        var result = await _engine.ExecuteAsync(context);

        Assert.True(result.Success);
        Assert.Equal(method, result.Output["authenticationMethod"]);
    }

    [Fact]
    public async Task Authenticate_EventPayload_ShouldContainCorrectFields()
    {
        var data = ValidAuthData();
        var identityId = data["identityId"] as string;
        var context = CreateContext(data);

        var result = await _engine.ExecuteAsync(context);

        var eventPayload = result.Events[0].Payload;
        Assert.Equal(identityId, eventPayload["identityId"]);
        Assert.Equal("Password", eventPayload["authenticationMethod"]);
        Assert.Equal("device-001", eventPayload["deviceId"]);
        Assert.Equal(1, eventPayload["eventVersion"]);
        Assert.Equal("whyce.identity.events", eventPayload["topic"]);
    }

    [Fact]
    public async Task Authenticate_MissingCredentialValue_ShouldFail()
    {
        var data = ValidAuthData();
        data.Remove("credentialValue");
        var context = CreateContext(data);

        var result = await _engine.ExecuteAsync(context);

        Assert.False(result.Success);
    }

    [Fact]
    public async Task Authenticate_MissingDeviceId_ShouldFail()
    {
        var data = ValidAuthData();
        data.Remove("deviceId");
        var context = CreateContext(data);

        var result = await _engine.ExecuteAsync(context);

        Assert.False(result.Success);
    }

    [Fact]
    public async Task ConcurrentAuthentications_ShouldAllComplete()
    {
        var tasks = new Task<EngineResult>[100];

        for (int i = 0; i < tasks.Length; i++)
        {
            var index = i;
            tasks[i] = Task.Run(async () =>
            {
                var methods = new[] { "Password", "Token", "OAuth", "APIKey" };
                var data = ValidAuthData();
                data["identityId"] = Guid.NewGuid().ToString();
                data["authenticationMethod"] = methods[index % 4];
                var context = CreateContext(data);
                return await _engine.ExecuteAsync(context);
            });
        }

        var results = await Task.WhenAll(tasks);

        Assert.All(results, r => Assert.True(r.Success));
    }
}
