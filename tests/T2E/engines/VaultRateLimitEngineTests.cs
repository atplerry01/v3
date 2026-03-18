namespace Whycespace.Tests.Engines;

using Whycespace.Engines.T2E.Economic.Vault.RateLimit.Engines;
using Whycespace.Contracts.Engines;
using Xunit;

public sealed class VaultRateLimitEngineTests
{
    private readonly VaultRateLimitEngine _engine = new();

    private static Dictionary<string, object> ValidData(string operationType = "Transfer", int currentCount = 0) => new()
    {
        ["vaultId"] = Guid.NewGuid().ToString(),
        ["vaultAccountId"] = Guid.NewGuid().ToString(),
        ["initiatorIdentityId"] = Guid.NewGuid().ToString(),
        ["operationType"] = operationType,
        ["currentOperationCount"] = currentCount
    };

    private static EngineContext CreateContext(Dictionary<string, object> data) =>
        new(Guid.NewGuid(), Guid.NewGuid().ToString(), "EvaluateRateLimit", "partition-1", data);

    // --- AllowedOperationTest ---

    [Fact]
    public async Task OperationBelowLimit_IsAllowed()
    {
        var data = ValidData("Transfer", 5);
        var result = await _engine.ExecuteAsync(CreateContext(data));

        Assert.True(result.Success);
        Assert.Equal(true, result.Output["isAllowed"]);
        Assert.Equal("Allowed", result.Output["rateLimitStatus"]);
    }

    [Theory]
    [InlineData("Transfer", 0)]
    [InlineData("Withdrawal", 0)]
    [InlineData("Contribution", 0)]
    [InlineData("Distribution", 0)]
    [InlineData("ProfitDistribution", 0)]
    [InlineData("Adjustment", 0)]
    [InlineData("Refund", 0)]
    public async Task AllOperationTypes_AllowedWhenBelowLimit(string operationType, int count)
    {
        var data = ValidData(operationType, count);
        var result = await _engine.ExecuteAsync(CreateContext(data));

        Assert.True(result.Success);
        Assert.Equal(true, result.Output["isAllowed"]);
    }

    // --- WarningThresholdTest ---

    [Fact]
    public async Task TransferAtWarningThreshold_ReturnsWarning()
    {
        // Transfer limit: 20/hour, warning at 80% = 16
        var data = ValidData("Transfer", 16);
        var result = await _engine.ExecuteAsync(CreateContext(data));

        Assert.True(result.Success);
        Assert.Equal(true, result.Output["isAllowed"]);
        Assert.Equal("Warning", result.Output["rateLimitStatus"]);
    }

    [Fact]
    public async Task WithdrawalAtWarningThreshold_ReturnsWarning()
    {
        // Withdrawal limit: 5/hour, warning at 80% = 4
        var data = ValidData("Withdrawal", 4);
        var result = await _engine.ExecuteAsync(CreateContext(data));

        Assert.True(result.Success);
        Assert.Equal(true, result.Output["isAllowed"]);
        Assert.Equal("Warning", result.Output["rateLimitStatus"]);
    }

    [Fact]
    public async Task WarningEvent_IsEmitted()
    {
        var data = ValidData("Transfer", 16);
        var result = await _engine.ExecuteAsync(CreateContext(data));

        Assert.True(result.Success);
        Assert.Equal(2, result.Events.Count);
        Assert.Equal("VaultRateLimitEvaluationRequested", result.Events[0].EventType);
        Assert.Equal("VaultRateLimitWarning", result.Events[1].EventType);
    }

    // --- RateLimitExceededTest ---

    [Fact]
    public async Task TransferExceedsLimit_IsBlocked()
    {
        // Transfer limit: 20/hour — count of 20 means next would be 21st
        var data = ValidData("Transfer", 20);
        var result = await _engine.ExecuteAsync(CreateContext(data));

        Assert.True(result.Success);
        Assert.Equal(false, result.Output["isAllowed"]);
        Assert.Equal("Blocked", result.Output["rateLimitStatus"]);
    }

    [Fact]
    public async Task WithdrawalExceedsLimit_IsBlocked()
    {
        // Withdrawal limit: 5/hour — 5 already done means 6th is blocked
        var data = ValidData("Withdrawal", 5);
        var result = await _engine.ExecuteAsync(CreateContext(data));

        Assert.True(result.Success);
        Assert.Equal(false, result.Output["isAllowed"]);
        Assert.Equal("Blocked", result.Output["rateLimitStatus"]);
    }

    [Fact]
    public async Task ExceededEvent_IsEmitted()
    {
        var data = ValidData("Withdrawal", 5);
        var result = await _engine.ExecuteAsync(CreateContext(data));

        Assert.True(result.Success);
        Assert.Equal(2, result.Events.Count);
        Assert.Equal("VaultRateLimitEvaluationRequested", result.Events[0].EventType);
        Assert.Equal("VaultRateLimitExceeded", result.Events[1].EventType);
    }

    // --- TimeWindowCalculationTest ---

    [Fact]
    public async Task TransferWindowDuration_IsOneHour()
    {
        var data = ValidData("Transfer", 5);
        var result = await _engine.ExecuteAsync(CreateContext(data));

        Assert.True(result.Success);
        Assert.Equal(60.0, result.Output["windowDurationMinutes"]);
    }

    [Fact]
    public async Task MaxAllowedOperations_MatchesRule()
    {
        var data = ValidData("Transfer", 5);
        var result = await _engine.ExecuteAsync(CreateContext(data));

        Assert.True(result.Success);
        Assert.Equal(20, result.Output["maxAllowedOperations"]);
    }

    [Fact]
    public async Task WithdrawalMaxAllowed_MatchesRule()
    {
        var data = ValidData("Withdrawal", 0);
        var result = await _engine.ExecuteAsync(CreateContext(data));

        Assert.True(result.Success);
        Assert.Equal(5, result.Output["maxAllowedOperations"]);
    }

    [Fact]
    public async Task ContributionMaxAllowed_MatchesRule()
    {
        var data = ValidData("Contribution", 0);
        var result = await _engine.ExecuteAsync(CreateContext(data));

        Assert.True(result.Success);
        Assert.Equal(100, result.Output["maxAllowedOperations"]);
    }

    // --- DeterministicEvaluationTest ---

    [Fact]
    public async Task SameInputs_ProduceSameDecision()
    {
        var data1 = new Dictionary<string, object>
        {
            ["vaultId"] = "11111111-1111-1111-1111-111111111111",
            ["vaultAccountId"] = "22222222-2222-2222-2222-222222222222",
            ["initiatorIdentityId"] = "33333333-3333-3333-3333-333333333333",
            ["operationType"] = "Transfer",
            ["currentOperationCount"] = 10
        };
        var data2 = new Dictionary<string, object>(data1);

        var result1 = await _engine.ExecuteAsync(CreateContext(data1));
        var result2 = await _engine.ExecuteAsync(CreateContext(data2));

        Assert.Equal(result1.Output["isAllowed"], result2.Output["isAllowed"]);
        Assert.Equal(result1.Output["rateLimitStatus"], result2.Output["rateLimitStatus"]);
        Assert.Equal(result1.Output["maxAllowedOperations"], result2.Output["maxAllowedOperations"]);
    }

    [Fact]
    public async Task BlockedDecision_IsDeterministic()
    {
        var data1 = new Dictionary<string, object>
        {
            ["vaultId"] = "11111111-1111-1111-1111-111111111111",
            ["vaultAccountId"] = "22222222-2222-2222-2222-222222222222",
            ["initiatorIdentityId"] = "33333333-3333-3333-3333-333333333333",
            ["operationType"] = "Withdrawal",
            ["currentOperationCount"] = 5
        };
        var data2 = new Dictionary<string, object>(data1);

        var result1 = await _engine.ExecuteAsync(CreateContext(data1));
        var result2 = await _engine.ExecuteAsync(CreateContext(data2));

        Assert.Equal(false, result1.Output["isAllowed"]);
        Assert.Equal(false, result2.Output["isAllowed"]);
        Assert.Equal("Blocked", result1.Output["rateLimitStatus"]);
        Assert.Equal("Blocked", result2.Output["rateLimitStatus"]);
    }

    // --- Input validation tests ---

    [Fact]
    public async Task MissingVaultId_Rejected()
    {
        var data = ValidData();
        data.Remove("vaultId");

        var result = await _engine.ExecuteAsync(CreateContext(data));
        Assert.False(result.Success);
        Assert.Contains("vaultId", result.Output["error"] as string);
    }

    [Fact]
    public async Task EmptyVaultId_Rejected()
    {
        var data = ValidData();
        data["vaultId"] = Guid.Empty.ToString();

        var result = await _engine.ExecuteAsync(CreateContext(data));
        Assert.False(result.Success);
    }

    [Fact]
    public async Task MissingOperationType_Rejected()
    {
        var data = ValidData();
        data.Remove("operationType");

        var result = await _engine.ExecuteAsync(CreateContext(data));
        Assert.False(result.Success);
        Assert.Contains("operationType", result.Output["error"] as string);
    }

    [Fact]
    public async Task UnsupportedOperationType_Rejected()
    {
        var data = ValidData();
        data["operationType"] = "InvalidOperation";

        var result = await _engine.ExecuteAsync(CreateContext(data));
        Assert.False(result.Success);
        Assert.Contains("Unsupported operationType", result.Output["error"] as string);
    }

    [Fact]
    public async Task MissingVaultAccountId_Rejected()
    {
        var data = ValidData();
        data.Remove("vaultAccountId");

        var result = await _engine.ExecuteAsync(CreateContext(data));
        Assert.False(result.Success);
    }

    [Fact]
    public async Task MissingInitiatorIdentityId_Rejected()
    {
        var data = ValidData();
        data.Remove("initiatorIdentityId");

        var result = await _engine.ExecuteAsync(CreateContext(data));
        Assert.False(result.Success);
    }

    [Fact]
    public async Task MissingCurrentOperationCount_Rejected()
    {
        var data = ValidData();
        data.Remove("currentOperationCount");

        var result = await _engine.ExecuteAsync(CreateContext(data));
        Assert.False(result.Success);
        Assert.Contains("currentOperationCount", result.Output["error"] as string);
    }

    // --- Event topic test ---

    [Fact]
    public async Task AllEvents_TargetEconomicTopic()
    {
        var result = await _engine.ExecuteAsync(CreateContext(ValidData()));

        Assert.True(result.Success);
        Assert.All(result.Events, e =>
            Assert.Equal("whyce.economic.events", e.Payload["topic"]));
    }

    // --- Passed event test ---

    [Fact]
    public async Task AllowedOperation_EmitsPassedEvent()
    {
        var data = ValidData("Transfer", 5);
        var result = await _engine.ExecuteAsync(CreateContext(data));

        Assert.True(result.Success);
        Assert.Equal(2, result.Events.Count);
        Assert.Equal("VaultRateLimitEvaluationRequested", result.Events[0].EventType);
        Assert.Equal("VaultRateLimitEvaluationPassed", result.Events[1].EventType);
    }
}
