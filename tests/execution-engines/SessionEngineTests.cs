namespace Whycespace.ExecutionEngines.Tests;

using Whycespace.Engines.T2E.Identity.Engines;
using Whycespace.Contracts.Engines;

public sealed class SessionEngineTests
{
    private readonly SessionEngine _engine = new();

    private static EngineContext CreateContext(Dictionary<string, object> data)
    {
        return new EngineContext(
            Guid.NewGuid(), Guid.NewGuid().ToString(), "Session",
            "partition-1", data);
    }

    private static Dictionary<string, object> ValidCreateSessionData()
    {
        return new Dictionary<string, object>
        {
            ["operation"] = "create",
            ["identityId"] = Guid.NewGuid().ToString(),
            ["deviceId"] = "device-001",
            ["authenticationMethod"] = "Password",
            ["authenticated"] = true,
            ["identityStatus"] = "Active",
            ["deviceTrustScore"] = 0.85,
            ["ipAddress"] = "192.168.1.1",
            ["geoLocation"] = "London, UK"
        };
    }

    private static Dictionary<string, object> ValidValidateSessionData()
    {
        return new Dictionary<string, object>
        {
            ["operation"] = "validate",
            ["sessionId"] = Guid.NewGuid().ToString(),
            ["identityId"] = Guid.NewGuid().ToString(),
            ["sessionActive"] = true,
            ["expiresAt"] = DateTime.UtcNow.AddHours(4).ToString("O"),
            ["identityRevoked"] = false,
            ["deviceRiskDetected"] = false
        };
    }

    // --- CreateSession tests ---

    [Fact]
    public async Task CreateSession_ValidData_ShouldSucceed()
    {
        var data = ValidCreateSessionData();
        var context = CreateContext(data);

        var result = await _engine.ExecuteAsync(context);

        Assert.True(result.Success);
        Assert.Single(result.Events);
        Assert.Equal("SessionCreated", result.Events[0].EventType);
        Assert.Equal(true, result.Output["active"]);
        Assert.Equal(data["identityId"], result.Output["identityId"]);
        Assert.NotNull(result.Output["sessionId"]);
        Assert.NotNull(result.Output["sessionToken"]);
    }

    [Fact]
    public async Task CreateSession_NotAuthenticated_ShouldFail()
    {
        var data = ValidCreateSessionData();
        data["authenticated"] = false;
        var context = CreateContext(data);

        var result = await _engine.ExecuteAsync(context);

        Assert.False(result.Success);
    }

    [Fact]
    public async Task CreateSession_InactiveIdentity_ShouldFail()
    {
        var data = ValidCreateSessionData();
        data["identityStatus"] = "Suspended";
        var context = CreateContext(data);

        var result = await _engine.ExecuteAsync(context);

        Assert.False(result.Success);
    }

    [Fact]
    public async Task CreateSession_LowDeviceTrust_ShouldFail()
    {
        var data = ValidCreateSessionData();
        data["deviceTrustScore"] = 0.3;
        var context = CreateContext(data);

        var result = await _engine.ExecuteAsync(context);

        Assert.False(result.Success);
    }

    [Fact]
    public async Task CreateSession_MissingIdentityId_ShouldFail()
    {
        var data = ValidCreateSessionData();
        data.Remove("identityId");
        var context = CreateContext(data);

        var result = await _engine.ExecuteAsync(context);

        Assert.False(result.Success);
    }

    [Fact]
    public async Task CreateSession_MissingDeviceId_ShouldFail()
    {
        var data = ValidCreateSessionData();
        data.Remove("deviceId");
        var context = CreateContext(data);

        var result = await _engine.ExecuteAsync(context);

        Assert.False(result.Success);
    }

    [Fact]
    public async Task CreateSession_InvalidAuthMethod_ShouldFail()
    {
        var data = ValidCreateSessionData();
        data["authenticationMethod"] = "InvalidMethod";
        var context = CreateContext(data);

        var result = await _engine.ExecuteAsync(context);

        Assert.False(result.Success);
    }

    [Fact]
    public async Task CreateSession_EventPayload_ShouldContainCorrectFields()
    {
        var data = ValidCreateSessionData();
        var identityId = data["identityId"] as string;
        var context = CreateContext(data);

        var result = await _engine.ExecuteAsync(context);

        var payload = result.Events[0].Payload;
        Assert.Equal(identityId, payload["identityId"]);
        Assert.Equal("device-001", payload["deviceId"]);
        Assert.Equal("Password", payload["authenticationMethod"]);
        Assert.Equal(1, payload["eventVersion"]);
        Assert.Equal("whyce.identity.events", payload["topic"]);
    }

    [Fact]
    public async Task CreateSession_ExpiresAt_ShouldBe8HoursAfterIssuedAt()
    {
        var data = ValidCreateSessionData();
        var context = CreateContext(data);

        var result = await _engine.ExecuteAsync(context);

        var issuedAt = DateTime.Parse(result.Output["issuedAt"] as string ?? "");
        var expiresAt = DateTime.Parse(result.Output["expiresAt"] as string ?? "");
        var duration = expiresAt - issuedAt;

        Assert.Equal(8, duration.TotalHours, 0);
    }

    // --- ValidateSession tests ---

    [Fact]
    public async Task ValidateSession_ActiveSession_ShouldSucceed()
    {
        var data = ValidValidateSessionData();
        var context = CreateContext(data);

        var result = await _engine.ExecuteAsync(context);

        Assert.True(result.Success);
        Assert.Single(result.Events);
        Assert.Equal("SessionValidated", result.Events[0].EventType);
        Assert.Equal(true, result.Output["valid"]);
    }

    [Fact]
    public async Task ValidateSession_ExpiredSession_ShouldFail()
    {
        var data = ValidValidateSessionData();
        data["expiresAt"] = DateTime.UtcNow.AddHours(-1).ToString("O");
        var context = CreateContext(data);

        var result = await _engine.ExecuteAsync(context);

        Assert.False(result.Success);
        Assert.Equal(false, result.Output["valid"]);
        Assert.Equal("Session has expired", result.Output["reason"]);
    }

    [Fact]
    public async Task ValidateSession_InactiveSession_ShouldFail()
    {
        var data = ValidValidateSessionData();
        data["sessionActive"] = false;
        var context = CreateContext(data);

        var result = await _engine.ExecuteAsync(context);

        Assert.False(result.Success);
        Assert.Equal(false, result.Output["valid"]);
        Assert.Equal("Session is not active", result.Output["reason"]);
    }

    [Fact]
    public async Task ValidateSession_RevokedIdentity_ShouldFail()
    {
        var data = ValidValidateSessionData();
        data["identityRevoked"] = true;
        var context = CreateContext(data);

        var result = await _engine.ExecuteAsync(context);

        Assert.False(result.Success);
        Assert.Equal(false, result.Output["valid"]);
        Assert.Equal("Identity has been revoked", result.Output["reason"]);
    }

    [Fact]
    public async Task ValidateSession_DeviceRiskDetected_ShouldFail()
    {
        var data = ValidValidateSessionData();
        data["deviceRiskDetected"] = true;
        var context = CreateContext(data);

        var result = await _engine.ExecuteAsync(context);

        Assert.False(result.Success);
        Assert.Equal(false, result.Output["valid"]);
        Assert.Equal("Device risk detected", result.Output["reason"]);
    }

    [Fact]
    public async Task ValidateSession_MissingSessionId_ShouldFail()
    {
        var data = ValidValidateSessionData();
        data.Remove("sessionId");
        var context = CreateContext(data);

        var result = await _engine.ExecuteAsync(context);

        Assert.False(result.Success);
    }

    // --- RevokeSession tests ---

    [Fact]
    public async Task RevokeSession_ValidSession_ShouldSucceed()
    {
        var sessionId = Guid.NewGuid().ToString();
        var identityId = Guid.NewGuid().ToString();
        var data = new Dictionary<string, object>
        {
            ["operation"] = "revoke",
            ["sessionId"] = sessionId,
            ["identityId"] = identityId
        };
        var context = CreateContext(data);

        var result = await _engine.ExecuteAsync(context);

        Assert.True(result.Success);
        Assert.Single(result.Events);
        Assert.Equal("SessionRevoked", result.Events[0].EventType);
        Assert.Equal(true, result.Output["revoked"]);
        Assert.Equal(sessionId, result.Output["sessionId"]);
    }

    [Fact]
    public async Task RevokeSession_MissingSessionId_ShouldFail()
    {
        var data = new Dictionary<string, object>
        {
            ["operation"] = "revoke",
            ["identityId"] = Guid.NewGuid().ToString()
        };
        var context = CreateContext(data);

        var result = await _engine.ExecuteAsync(context);

        Assert.False(result.Success);
    }

    // --- Concurrency test ---

    [Fact]
    public async Task ConcurrentSessionCreations_ShouldAllComplete()
    {
        var tasks = new Task<EngineResult>[100];

        for (int i = 0; i < tasks.Length; i++)
        {
            var index = i;
            tasks[i] = Task.Run(async () =>
            {
                var methods = new[] { "Password", "Token", "OAuth", "APIKey" };
                var data = ValidCreateSessionData();
                data["identityId"] = Guid.NewGuid().ToString();
                data["authenticationMethod"] = methods[index % 4];
                var context = CreateContext(data);
                return await _engine.ExecuteAsync(context);
            });
        }

        var results = await Task.WhenAll(tasks);

        Assert.All(results, r => Assert.True(r.Success));

        var sessionIds = results
            .Select(r => r.Output["sessionId"] as string)
            .ToHashSet();
        Assert.Equal(100, sessionIds.Count);
    }

    // --- Default operation test ---

    [Fact]
    public async Task DefaultOperation_ShouldBeCreate()
    {
        var data = ValidCreateSessionData();
        data.Remove("operation");
        var context = CreateContext(data);

        var result = await _engine.ExecuteAsync(context);

        Assert.True(result.Success);
        Assert.Equal("SessionCreated", result.Events[0].EventType);
    }

    [Fact]
    public async Task UnknownOperation_ShouldFail()
    {
        var data = new Dictionary<string, object>
        {
            ["operation"] = "unknown"
        };
        var context = CreateContext(data);

        var result = await _engine.ExecuteAsync(context);

        Assert.False(result.Success);
    }
}
