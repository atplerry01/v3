namespace Whycespace.ExecutionEngines.Tests;

using Whycespace.Engines.T2E.Identity.Engines;
using Whycespace.Contracts.Engines;

public sealed class ConsentEngineTests
{
    private readonly ConsentEngine _engine = new();

    private static EngineContext CreateContext(Dictionary<string, object> data)
    {
        return new EngineContext(
            Guid.NewGuid(), Guid.NewGuid().ToString(), "Consent",
            "partition-1", data);
    }

    private static Dictionary<string, object> ValidGrantData()
    {
        return new Dictionary<string, object>
        {
            ["operation"] = "grant",
            ["identityId"] = Guid.NewGuid().ToString(),
            ["consentType"] = "DataProcessing",
            ["consentScope"] = "analytics,marketing",
            ["grantedBy"] = "user-self",
            ["identityExists"] = true,
            ["identityStatus"] = "Active"
        };
    }

    private static Dictionary<string, object> ValidWithdrawData()
    {
        return new Dictionary<string, object>
        {
            ["operation"] = "withdraw",
            ["identityId"] = Guid.NewGuid().ToString(),
            ["consentType"] = "DataProcessing",
            ["reason"] = "User requested withdrawal",
            ["consentGranted"] = true
        };
    }

    private static Dictionary<string, object> ValidValidateData()
    {
        return new Dictionary<string, object>
        {
            ["operation"] = "validate",
            ["identityId"] = Guid.NewGuid().ToString(),
            ["consentType"] = "DataProcessing",
            ["requiredScope"] = "analytics",
            ["identityExists"] = true,
            ["consentGranted"] = true,
            ["consentWithdrawn"] = false,
            ["consentScope"] = "analytics,marketing"
        };
    }

    // --- Grant Consent Tests ---

    [Fact]
    public async Task GrantConsent_ValidCommand_ShouldSucceed()
    {
        var data = ValidGrantData();
        var context = CreateContext(data);

        var result = await _engine.ExecuteAsync(context);

        Assert.True(result.Success);
        Assert.Single(result.Events);
        Assert.Equal("ConsentGranted", result.Events[0].EventType);
        Assert.Equal(true, result.Output["granted"]);
        Assert.Equal(data["identityId"], result.Output["identityId"]);
        Assert.Equal("DataProcessing", result.Output["consentType"]);
        Assert.Equal("analytics,marketing", result.Output["scope"]);
    }

    [Fact]
    public async Task GrantConsent_MissingIdentityId_ShouldFail()
    {
        var data = ValidGrantData();
        data.Remove("identityId");
        var context = CreateContext(data);

        var result = await _engine.ExecuteAsync(context);

        Assert.False(result.Success);
    }

    [Fact]
    public async Task GrantConsent_InvalidConsentType_ShouldFail()
    {
        var data = ValidGrantData();
        data["consentType"] = "InvalidType";
        var context = CreateContext(data);

        var result = await _engine.ExecuteAsync(context);

        Assert.False(result.Success);
    }

    [Fact]
    public async Task GrantConsent_InactiveIdentity_ShouldFail()
    {
        var data = ValidGrantData();
        data["identityStatus"] = "Suspended";
        var context = CreateContext(data);

        var result = await _engine.ExecuteAsync(context);

        Assert.False(result.Success);
    }

    [Fact]
    public async Task GrantConsent_NonExistentIdentity_ShouldFail()
    {
        var data = ValidGrantData();
        data["identityExists"] = false;
        var context = CreateContext(data);

        var result = await _engine.ExecuteAsync(context);

        Assert.False(result.Success);
    }

    [Fact]
    public async Task GrantConsent_PolicyDenied_ShouldFail()
    {
        var data = ValidGrantData();
        data["policyDenied"] = true;
        var context = CreateContext(data);

        var result = await _engine.ExecuteAsync(context);

        Assert.False(result.Success);
    }

    [Fact]
    public async Task GrantConsent_EventPayload_ShouldContainCorrectFields()
    {
        var data = ValidGrantData();
        var identityId = data["identityId"] as string;
        var context = CreateContext(data);

        var result = await _engine.ExecuteAsync(context);

        var eventPayload = result.Events[0].Payload;
        Assert.Equal(identityId, eventPayload["identityId"]);
        Assert.Equal("DataProcessing", eventPayload["consentType"]);
        Assert.Equal("analytics,marketing", eventPayload["scope"]);
        Assert.Equal("user-self", eventPayload["grantedBy"]);
        Assert.Equal(1, eventPayload["eventVersion"]);
        Assert.Equal("whyce.identity.events", eventPayload["topic"]);
    }

    [Theory]
    [InlineData("DataProcessing")]
    [InlineData("Marketing")]
    [InlineData("Analytics")]
    [InlineData("ThirdPartySharing")]
    [InlineData("Profiling")]
    [InlineData("SystemOperations")]
    public async Task GrantConsent_AllValidTypes_ShouldSucceed(string consentType)
    {
        var data = ValidGrantData();
        data["consentType"] = consentType;
        var context = CreateContext(data);

        var result = await _engine.ExecuteAsync(context);

        Assert.True(result.Success);
        Assert.Equal(consentType, result.Output["consentType"]);
    }

    // --- Withdraw Consent Tests ---

    [Fact]
    public async Task WithdrawConsent_ValidCommand_ShouldSucceed()
    {
        var data = ValidWithdrawData();
        var context = CreateContext(data);

        var result = await _engine.ExecuteAsync(context);

        Assert.True(result.Success);
        Assert.Single(result.Events);
        Assert.Equal("ConsentWithdrawn", result.Events[0].EventType);
        Assert.Equal(false, result.Output["granted"]);
        Assert.Equal(data["identityId"], result.Output["identityId"]);
    }

    [Fact]
    public async Task WithdrawConsent_NotPreviouslyGranted_ShouldFail()
    {
        var data = ValidWithdrawData();
        data["consentGranted"] = false;
        var context = CreateContext(data);

        var result = await _engine.ExecuteAsync(context);

        Assert.False(result.Success);
    }

    [Fact]
    public async Task WithdrawConsent_MissingReason_ShouldFail()
    {
        var data = ValidWithdrawData();
        data.Remove("reason");
        var context = CreateContext(data);

        var result = await _engine.ExecuteAsync(context);

        Assert.False(result.Success);
    }

    // --- Validate Consent Tests ---

    [Fact]
    public async Task ValidateConsent_GrantedConsent_ShouldSucceed()
    {
        var data = ValidValidateData();
        var context = CreateContext(data);

        var result = await _engine.ExecuteAsync(context);

        Assert.True(result.Success);
        Assert.Single(result.Events);
        Assert.Equal("ConsentValidated", result.Events[0].EventType);
        Assert.Equal(true, result.Output["valid"]);
        Assert.Equal(true, result.Output["scopeValidated"]);
    }

    [Fact]
    public async Task ValidateConsent_WithdrawnConsent_ShouldFail()
    {
        var data = ValidValidateData();
        data["consentWithdrawn"] = true;
        var context = CreateContext(data);

        var result = await _engine.ExecuteAsync(context);

        Assert.False(result.Success);
        Assert.Equal(false, result.Output["valid"]);
        Assert.Equal("Consent withdrawn", result.Output["reason"]);
    }

    [Fact]
    public async Task ValidateConsent_NotGranted_ShouldFail()
    {
        var data = ValidValidateData();
        data["consentGranted"] = false;
        var context = CreateContext(data);

        var result = await _engine.ExecuteAsync(context);

        Assert.False(result.Success);
        Assert.Equal(false, result.Output["valid"]);
        Assert.Equal("Consent not granted", result.Output["reason"]);
    }

    [Fact]
    public async Task ValidateConsent_ScopeMismatch_ShouldFail()
    {
        var data = ValidValidateData();
        data["requiredScope"] = "financial";
        var context = CreateContext(data);

        var result = await _engine.ExecuteAsync(context);

        Assert.False(result.Success);
        Assert.Equal(false, result.Output["valid"]);
        Assert.Equal("Scope mismatch", result.Output["reason"]);
    }

    [Fact]
    public async Task ValidateConsent_PolicyRestriction_ShouldFail()
    {
        var data = ValidValidateData();
        data["policyDenied"] = true;
        var context = CreateContext(data);

        var result = await _engine.ExecuteAsync(context);

        Assert.False(result.Success);
        Assert.Equal(false, result.Output["valid"]);
        Assert.Equal("Policy restriction", result.Output["reason"]);
    }

    [Fact]
    public async Task ValidateConsent_NonExistentIdentity_ShouldFail()
    {
        var data = ValidValidateData();
        data["identityExists"] = false;
        var context = CreateContext(data);

        var result = await _engine.ExecuteAsync(context);

        Assert.False(result.Success);
        Assert.Equal(false, result.Output["valid"]);
        Assert.Equal("Identity does not exist", result.Output["reason"]);
    }

    // --- Unknown Operation ---

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

    // --- Concurrency Test ---

    [Fact]
    public async Task ConcurrentConsentOperations_ShouldAllComplete()
    {
        var tasks = new Task<EngineResult>[100];

        for (int i = 0; i < tasks.Length; i++)
        {
            var index = i;
            tasks[i] = Task.Run(async () =>
            {
                var consentTypes = new[] { "DataProcessing", "Marketing", "Analytics", "ThirdPartySharing" };
                var data = ValidGrantData();
                data["identityId"] = Guid.NewGuid().ToString();
                data["consentType"] = consentTypes[index % 4];
                var context = CreateContext(data);
                return await _engine.ExecuteAsync(context);
            });
        }

        var results = await Task.WhenAll(tasks);

        Assert.All(results, r => Assert.True(r.Success));
    }
}
