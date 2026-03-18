namespace Whycespace.ExecutionEngines.Tests;

using Whycespace.Engines.T2E.Economic.Vault.Risk.Engines;
using Whycespace.Contracts.Engines;

public sealed class VaultRiskControlEngineTests
{
    private readonly VaultRiskControlEngine _engine = new();

    private static EngineContext CreateContext(Dictionary<string, object> data)
    {
        return new EngineContext(
            Guid.NewGuid(), Guid.NewGuid().ToString(), "EvaluateVaultRisk",
            "partition-1", data);
    }

    private static Dictionary<string, object> ValidData(decimal amount = 1000m) => new()
    {
        ["vaultId"] = Guid.NewGuid().ToString(),
        ["vaultAccountId"] = Guid.NewGuid().ToString(),
        ["transactionId"] = Guid.NewGuid().ToString(),
        ["initiatorIdentityId"] = Guid.NewGuid().ToString(),
        ["operationType"] = "Contribution",
        ["amount"] = amount,
        ["currency"] = "GBP",
        ["requestedAt"] = DateTime.UtcNow.ToString("O"),
        ["vaultBalance"] = 100_000m
    };

    // --- Low Risk Transaction Tests ---

    [Fact]
    public async Task AllowLowRiskTransaction_WhenSmallAmount()
    {
        var data = ValidData(1000m);
        var result = await _engine.ExecuteAsync(CreateContext(data));

        Assert.True(result.Success);
        Assert.Equal(true, result.Output["isAllowed"]);
        Assert.Equal("Low", result.Output["riskLevel"]);
        Assert.Equal("Approved", result.Output["riskDecision"]);
    }

    [Fact]
    public async Task EmitPassedEvent_WhenLowRisk()
    {
        var data = ValidData(500m);
        var result = await _engine.ExecuteAsync(CreateContext(data));

        Assert.Equal(2, result.Events.Count);
        Assert.Equal("VaultRiskEvaluationRequested", result.Events[0].EventType);
        Assert.Equal("VaultRiskEvaluationPassed", result.Events[1].EventType);
        Assert.Equal("whyce.economic.events", result.Events[1].Payload["topic"]);
    }

    // --- Medium Risk Transaction Tests ---

    [Fact]
    public async Task FlagMediumRiskTransaction_WhenModerateAmountWithFrequency()
    {
        var data = ValidData(60_000m);
        data["recentTransactionCount"] = 25;
        var result = await _engine.ExecuteAsync(CreateContext(data));

        Assert.True(result.Success);
        Assert.Equal(true, result.Output["isAllowed"]);
        Assert.Equal("Medium", result.Output["riskLevel"]);
        Assert.Equal("ApprovedWithMonitoring", result.Output["riskDecision"]);
    }

    [Fact]
    public async Task EmitFlaggedEvent_WhenMediumRisk()
    {
        var data = ValidData(60_000m);
        data["recentTransactionCount"] = 25;
        var result = await _engine.ExecuteAsync(CreateContext(data));

        Assert.Equal(2, result.Events.Count);
        Assert.Equal("VaultRiskEvaluationRequested", result.Events[0].EventType);
        Assert.Equal("VaultRiskEvaluationFlagged", result.Events[1].EventType);
    }

    // --- High Risk Transaction Tests ---

    [Fact]
    public async Task BlockHighRiskTransaction_WhenLargeWithdrawalExceedsBalance()
    {
        var data = ValidData(60_000m);
        data["operationType"] = "Withdrawal";
        data["vaultBalance"] = 100_000m;
        data["recentTransactionCount"] = 55;
        data["behaviorFlag"] = "suspicious";
        var result = await _engine.ExecuteAsync(CreateContext(data));

        Assert.True(result.Success);
        Assert.Equal(false, result.Output["isAllowed"]);
        Assert.Equal("High", result.Output["riskLevel"]);
    }

    [Fact]
    public async Task BlockWithdrawal_WhenExceeds50PercentOfBalance()
    {
        var data = ValidData(300_000m);
        data["operationType"] = "Withdrawal";
        data["vaultBalance"] = 500_000m;
        data["recentTransactionCount"] = 55;
        data["behaviorFlag"] = "suspicious";
        var result = await _engine.ExecuteAsync(CreateContext(data));

        Assert.True(result.Success);
        Assert.Equal(false, result.Output["isAllowed"]);
        Assert.Equal("Blocked", result.Output["riskDecision"]);
    }

    [Fact]
    public async Task EmitBlockedEvent_WhenHighRisk()
    {
        var data = ValidData(300_000m);
        data["operationType"] = "Withdrawal";
        data["vaultBalance"] = 500_000m;
        data["recentTransactionCount"] = 55;
        data["behaviorFlag"] = "suspicious";
        var result = await _engine.ExecuteAsync(CreateContext(data));

        Assert.Equal(2, result.Events.Count);
        Assert.Equal("VaultRiskEvaluationRequested", result.Events[0].EventType);
        Assert.Equal("VaultRiskEvaluationBlocked", result.Events[1].EventType);
    }

    // --- Risk Score Calculation Tests ---

    [Fact]
    public async Task CalculateRiskScore_Deterministically()
    {
        var data = ValidData(25_000m);
        data["recentTransactionCount"] = 15;
        var context = CreateContext(data);

        var result1 = await _engine.ExecuteAsync(context);
        var result2 = await _engine.ExecuteAsync(context);

        Assert.True(result1.Success);
        Assert.True(result2.Success);
        Assert.Equal(result1.Output["riskScore"], result2.Output["riskScore"]);
        Assert.Equal(result1.Output["riskLevel"], result2.Output["riskLevel"]);
        Assert.Equal(result1.Output["riskDecision"], result2.Output["riskDecision"]);
    }

    [Fact]
    public async Task RiskScoreIncreasesWithAmount()
    {
        var smallData = ValidData(5_000m);
        var largeData = ValidData(300_000m);

        var smallResult = await _engine.ExecuteAsync(CreateContext(smallData));
        var largeResult = await _engine.ExecuteAsync(CreateContext(largeData));

        var smallScore = (decimal)smallResult.Output["riskScore"];
        var largeScore = (decimal)largeResult.Output["riskScore"];

        Assert.True(largeScore > smallScore, $"Large amount score ({largeScore}) should exceed small amount score ({smallScore})");
    }

    [Fact]
    public async Task RiskScoreIncreasesWithFrequency()
    {
        var lowFreq = ValidData(50_000m);
        lowFreq["recentTransactionCount"] = 5;

        var highFreq = ValidData(50_000m);
        highFreq["recentTransactionCount"] = 55;

        var lowResult = await _engine.ExecuteAsync(CreateContext(lowFreq));
        var highResult = await _engine.ExecuteAsync(CreateContext(highFreq));

        var lowScore = (decimal)lowResult.Output["riskScore"];
        var highScore = (decimal)highResult.Output["riskScore"];

        Assert.True(highScore > lowScore, $"High frequency score ({highScore}) should exceed low frequency score ({lowScore})");
    }

    [Fact]
    public async Task RiskScoreClampedTo100()
    {
        var data = ValidData(10_000_000m);
        data["operationType"] = "TreasuryWithdrawal";
        data["vaultBalance"] = 1_000m;
        data["recentTransactionCount"] = 100;
        data["behaviorFlag"] = "suspicious";
        var result = await _engine.ExecuteAsync(CreateContext(data));

        var score = (decimal)result.Output["riskScore"];
        Assert.True(score <= 100m, $"Risk score ({score}) must not exceed 100");
    }

    // --- Risk Decision Tests ---

    [Fact]
    public async Task ReturnCorrectDecision_ForLowRisk()
    {
        var data = ValidData(500m);
        var result = await _engine.ExecuteAsync(CreateContext(data));

        Assert.Equal("Approved", result.Output["riskDecision"]);
        Assert.Equal(true, result.Output["isAllowed"]);
    }

    [Fact]
    public async Task ReturnCorrectDecision_ForBlockedTransaction()
    {
        var data = ValidData(300_000m);
        data["operationType"] = "TreasuryWithdrawal";
        data["vaultBalance"] = 400_000m;
        data["recentTransactionCount"] = 55;
        data["behaviorFlag"] = "suspicious";
        var result = await _engine.ExecuteAsync(CreateContext(data));

        Assert.Equal(false, result.Output["isAllowed"]);
        Assert.Contains("Blocked", (string)result.Output["riskDecision"]);
    }

    // --- Behavior Flag Tests ---

    [Fact]
    public async Task SuspiciousBehaviorIncreasesRiskScore()
    {
        var normalData = ValidData(50_000m);
        var suspiciousData = ValidData(50_000m);
        suspiciousData["behaviorFlag"] = "suspicious";

        var normalResult = await _engine.ExecuteAsync(CreateContext(normalData));
        var suspiciousResult = await _engine.ExecuteAsync(CreateContext(suspiciousData));

        var normalScore = (decimal)normalResult.Output["riskScore"];
        var suspiciousScore = (decimal)suspiciousResult.Output["riskScore"];

        Assert.True(suspiciousScore > normalScore);
    }

    // --- Operation Type Modifier Tests ---

    [Fact]
    public async Task WithdrawalHasHigherRiskThanContribution()
    {
        var contribution = ValidData(50_000m);
        contribution["operationType"] = "Contribution";

        var withdrawal = ValidData(50_000m);
        withdrawal["operationType"] = "Withdrawal";

        var contribResult = await _engine.ExecuteAsync(CreateContext(contribution));
        var withdrawResult = await _engine.ExecuteAsync(CreateContext(withdrawal));

        var contribScore = (decimal)contribResult.Output["riskScore"];
        var withdrawScore = (decimal)withdrawResult.Output["riskScore"];

        Assert.True(withdrawScore > contribScore);
    }

    // --- Input Validation Tests ---

    [Fact]
    public async Task FailOnMissingVaultId()
    {
        var data = ValidData();
        data.Remove("vaultId");
        var result = await _engine.ExecuteAsync(CreateContext(data));
        Assert.False(result.Success);
    }

    [Fact]
    public async Task FailOnMissingTransactionId()
    {
        var data = ValidData();
        data.Remove("transactionId");
        var result = await _engine.ExecuteAsync(CreateContext(data));
        Assert.False(result.Success);
    }

    [Fact]
    public async Task FailOnInvalidOperationType()
    {
        var data = ValidData();
        data["operationType"] = "InvalidOp";
        var result = await _engine.ExecuteAsync(CreateContext(data));
        Assert.False(result.Success);
    }

    [Fact]
    public async Task FailOnNegativeAmount()
    {
        var data = ValidData();
        data["amount"] = -100m;
        var result = await _engine.ExecuteAsync(CreateContext(data));
        Assert.False(result.Success);
    }

    [Fact]
    public async Task FailOnInvalidCurrency()
    {
        var data = ValidData();
        data["currency"] = "BTC";
        var result = await _engine.ExecuteAsync(CreateContext(data));
        Assert.False(result.Success);
    }

    [Fact]
    public async Task FailOnMissingCurrency()
    {
        var data = ValidData();
        data.Remove("currency");
        var result = await _engine.ExecuteAsync(CreateContext(data));
        Assert.False(result.Success);
    }

    // --- Output Structure Tests ---

    [Fact]
    public async Task OutputContainsAllRequiredFields()
    {
        var data = ValidData(5000m);
        var result = await _engine.ExecuteAsync(CreateContext(data));

        Assert.True(result.Success);
        Assert.True(result.Output.ContainsKey("vaultId"));
        Assert.True(result.Output.ContainsKey("transactionId"));
        Assert.True(result.Output.ContainsKey("riskScore"));
        Assert.True(result.Output.ContainsKey("riskLevel"));
        Assert.True(result.Output.ContainsKey("isAllowed"));
        Assert.True(result.Output.ContainsKey("riskDecision"));
        Assert.True(result.Output.ContainsKey("riskReason"));
        Assert.True(result.Output.ContainsKey("evaluatedAt"));
    }
}
