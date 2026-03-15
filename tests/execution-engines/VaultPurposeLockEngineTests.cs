namespace Whycespace.Tests.ExecutionEngines;

using Whycespace.Engines.T2E.Economic.Vault;
using Whycespace.Contracts.Engines;
using Xunit;

public sealed class VaultPurposeLockEngineTests
{
    private readonly VaultPurposeLockEngine _engine = new();

    // --- Purpose Validation Success Tests ---

    [Fact]
    public async Task InvestmentCapital_AllowsContribution()
    {
        var result = await ExecuteValidation("InvestmentCapital", "Contribution");

        Assert.True(result.Success);
        Assert.Equal(true, result.Output["isAllowed"]);
        Assert.Contains("permitted", result.Output["validationReason"].ToString()!);
    }

    [Fact]
    public async Task InvestmentCapital_AllowsTransfer()
    {
        var result = await ExecuteValidation("InvestmentCapital", "Transfer");

        Assert.True(result.Success);
        Assert.Equal(true, result.Output["isAllowed"]);
    }

    [Fact]
    public async Task InvestmentCapital_AllowsProfitDistribution()
    {
        var result = await ExecuteValidation("InvestmentCapital", "ProfitDistribution");

        Assert.True(result.Success);
        Assert.Equal(true, result.Output["isAllowed"]);
    }

    [Fact]
    public async Task OperationalTreasury_AllowsWithdrawal()
    {
        var result = await ExecuteValidation("OperationalTreasury", "Withdrawal");

        Assert.True(result.Success);
        Assert.Equal(true, result.Output["isAllowed"]);
    }

    [Fact]
    public async Task GeneralPurpose_AllowsAllTransactions()
    {
        foreach (var txType in new[] { "Contribution", "Transfer", "Withdrawal", "Distribution", "ProfitDistribution", "Adjustment", "Refund" })
        {
            var result = await ExecuteValidation("GeneralPurpose", txType);
            Assert.True(result.Success);
            Assert.Equal(true, result.Output["isAllowed"]);
        }
    }

    // --- Purpose Validation Failure Tests ---

    [Fact]
    public async Task InvestmentCapital_RejectsWithdrawal()
    {
        var result = await ExecuteValidation("InvestmentCapital", "Withdrawal");

        Assert.True(result.Success);
        Assert.Equal(false, result.Output["isAllowed"]);
        Assert.Contains("restricted", result.Output["validationReason"].ToString()!);
    }

    [Fact]
    public async Task InvestmentCapital_RejectsDistribution()
    {
        var result = await ExecuteValidation("InvestmentCapital", "Distribution");

        Assert.True(result.Success);
        Assert.Equal(false, result.Output["isAllowed"]);
    }

    // --- Escrow Restriction Tests ---

    [Fact]
    public async Task Escrow_AllowsContribution()
    {
        var result = await ExecuteValidation("Escrow", "Contribution");

        Assert.True(result.Success);
        Assert.Equal(true, result.Output["isAllowed"]);
    }

    [Fact]
    public async Task Escrow_RejectsTransfer()
    {
        var result = await ExecuteValidation("Escrow", "Transfer");

        Assert.True(result.Success);
        Assert.Equal(false, result.Output["isAllowed"]);
    }

    [Fact]
    public async Task Escrow_RejectsWithdrawal()
    {
        var result = await ExecuteValidation("Escrow", "Withdrawal");

        Assert.True(result.Success);
        Assert.Equal(false, result.Output["isAllowed"]);
    }

    [Fact]
    public async Task Escrow_RejectsDistribution()
    {
        var result = await ExecuteValidation("Escrow", "Distribution");

        Assert.True(result.Success);
        Assert.Equal(false, result.Output["isAllowed"]);
    }

    // --- Infrastructure Purpose Tests ---

    [Fact]
    public async Task InfrastructureFunding_AllowsContribution()
    {
        var result = await ExecuteValidation("InfrastructureFunding", "Contribution");

        Assert.True(result.Success);
        Assert.Equal(true, result.Output["isAllowed"]);
    }

    [Fact]
    public async Task InfrastructureFunding_AllowsTransfer()
    {
        var result = await ExecuteValidation("InfrastructureFunding", "Transfer");

        Assert.True(result.Success);
        Assert.Equal(true, result.Output["isAllowed"]);
    }

    [Fact]
    public async Task InfrastructureFunding_RejectsWithdrawal()
    {
        var result = await ExecuteValidation("InfrastructureFunding", "Withdrawal");

        Assert.True(result.Success);
        Assert.Equal(false, result.Output["isAllowed"]);
    }

    [Fact]
    public async Task InfrastructureFunding_RejectsDistribution()
    {
        var result = await ExecuteValidation("InfrastructureFunding", "Distribution");

        Assert.True(result.Success);
        Assert.Equal(false, result.Output["isAllowed"]);
    }

    // --- Validation Result Content Tests ---

    [Fact]
    public async Task ValidationResult_ContainsCorrectReason_WhenAllowed()
    {
        var result = await ExecuteValidation("OperationalTreasury", "Contribution");

        Assert.True(result.Success);
        Assert.Equal(true, result.Output["isAllowed"]);
        var reason = result.Output["validationReason"].ToString()!;
        Assert.Contains("Contribution", reason);
        Assert.Contains("OperationalTreasury", reason);
        Assert.Contains("permitted", reason);
    }

    [Fact]
    public async Task ValidationResult_ContainsCorrectReason_WhenRejected()
    {
        var result = await ExecuteValidation("Escrow", "Withdrawal");

        Assert.True(result.Success);
        Assert.Equal(false, result.Output["isAllowed"]);
        var reason = result.Output["validationReason"].ToString()!;
        Assert.Contains("Withdrawal", reason);
        Assert.Contains("Escrow", reason);
        Assert.Contains("restricted", reason);
    }

    [Fact]
    public async Task ValidationResult_ContainsEvaluatedAt()
    {
        var result = await ExecuteValidation("GeneralPurpose", "Contribution");

        Assert.True(result.Success);
        Assert.True(result.Output.ContainsKey("evaluatedAt"));
        Assert.True(DateTime.TryParse(result.Output["evaluatedAt"].ToString(), out _));
    }

    // --- Event Tests ---

    [Fact]
    public async Task Allowed_EmitsValidationPassedEvent()
    {
        var result = await ExecuteValidation("OperationalTreasury", "Transfer");

        Assert.True(result.Success);
        Assert.Contains(result.Events, e => e.EventType == "VaultPurposeValidationRequested");
        Assert.Contains(result.Events, e => e.EventType == "VaultPurposeValidationPassed");
    }

    [Fact]
    public async Task Rejected_EmitsValidationFailedEvent()
    {
        var result = await ExecuteValidation("Escrow", "Transfer");

        Assert.True(result.Success);
        Assert.Contains(result.Events, e => e.EventType == "VaultPurposeValidationRequested");
        Assert.Contains(result.Events, e => e.EventType == "VaultPurposeValidationFailed");
    }

    [Fact]
    public async Task Events_ContainTopicField()
    {
        var result = await ExecuteValidation("GeneralPurpose", "Contribution");

        Assert.True(result.Success);
        foreach (var evt in result.Events)
        {
            Assert.Equal("whyce.economic.events", evt.Payload["topic"]);
        }
    }

    // --- Input Validation Tests ---

    [Fact]
    public async Task MissingVaultId_Fails()
    {
        var context = CreateContext(data: new Dictionary<string, object>
        {
            ["vaultPurpose"] = "GeneralPurpose",
            ["transactionType"] = "Contribution",
            ["amount"] = 100m,
            ["initiatorIdentityId"] = Guid.NewGuid().ToString()
        });

        var result = await _engine.ExecuteAsync(context);
        Assert.False(result.Success);
    }

    [Fact]
    public async Task MissingVaultPurpose_Fails()
    {
        var context = CreateContext(data: new Dictionary<string, object>
        {
            ["vaultId"] = Guid.NewGuid().ToString(),
            ["transactionType"] = "Contribution",
            ["amount"] = 100m,
            ["initiatorIdentityId"] = Guid.NewGuid().ToString()
        });

        var result = await _engine.ExecuteAsync(context);
        Assert.False(result.Success);
    }

    [Fact]
    public async Task InvalidVaultPurpose_Fails()
    {
        var context = CreateContext(data: new Dictionary<string, object>
        {
            ["vaultId"] = Guid.NewGuid().ToString(),
            ["vaultPurpose"] = "InvalidPurpose",
            ["transactionType"] = "Contribution",
            ["amount"] = 100m,
            ["initiatorIdentityId"] = Guid.NewGuid().ToString()
        });

        var result = await _engine.ExecuteAsync(context);
        Assert.False(result.Success);
    }

    [Fact]
    public async Task MissingTransactionType_Fails()
    {
        var context = CreateContext(data: new Dictionary<string, object>
        {
            ["vaultId"] = Guid.NewGuid().ToString(),
            ["vaultPurpose"] = "GeneralPurpose",
            ["amount"] = 100m,
            ["initiatorIdentityId"] = Guid.NewGuid().ToString()
        });

        var result = await _engine.ExecuteAsync(context);
        Assert.False(result.Success);
    }

    [Fact]
    public async Task InvalidTransactionType_Fails()
    {
        var context = CreateContext(data: new Dictionary<string, object>
        {
            ["vaultId"] = Guid.NewGuid().ToString(),
            ["vaultPurpose"] = "GeneralPurpose",
            ["transactionType"] = "InvalidType",
            ["amount"] = 100m,
            ["initiatorIdentityId"] = Guid.NewGuid().ToString()
        });

        var result = await _engine.ExecuteAsync(context);
        Assert.False(result.Success);
    }

    [Fact]
    public async Task ZeroAmount_Fails()
    {
        var context = CreateContext(data: new Dictionary<string, object>
        {
            ["vaultId"] = Guid.NewGuid().ToString(),
            ["vaultPurpose"] = "GeneralPurpose",
            ["transactionType"] = "Contribution",
            ["amount"] = 0m,
            ["initiatorIdentityId"] = Guid.NewGuid().ToString()
        });

        var result = await _engine.ExecuteAsync(context);
        Assert.False(result.Success);
    }

    [Fact]
    public async Task NegativeAmount_Fails()
    {
        var context = CreateContext(data: new Dictionary<string, object>
        {
            ["vaultId"] = Guid.NewGuid().ToString(),
            ["vaultPurpose"] = "GeneralPurpose",
            ["transactionType"] = "Contribution",
            ["amount"] = -500m,
            ["initiatorIdentityId"] = Guid.NewGuid().ToString()
        });

        var result = await _engine.ExecuteAsync(context);
        Assert.False(result.Success);
    }

    [Fact]
    public async Task MissingInitiatorIdentityId_Fails()
    {
        var context = CreateContext(data: new Dictionary<string, object>
        {
            ["vaultId"] = Guid.NewGuid().ToString(),
            ["vaultPurpose"] = "GeneralPurpose",
            ["transactionType"] = "Contribution",
            ["amount"] = 100m
        });

        var result = await _engine.ExecuteAsync(context);
        Assert.False(result.Success);
    }

    // --- Helper methods ---

    private async Task<EngineResult> ExecuteValidation(string vaultPurpose, string transactionType)
    {
        var context = CreateContext(data: new Dictionary<string, object>
        {
            ["vaultId"] = Guid.NewGuid().ToString(),
            ["vaultPurpose"] = vaultPurpose,
            ["transactionType"] = transactionType,
            ["amount"] = 1000m,
            ["initiatorIdentityId"] = Guid.NewGuid().ToString()
        });

        return await _engine.ExecuteAsync(context);
    }

    private static EngineContext CreateContext(Dictionary<string, object> data)
    {
        return new EngineContext(
            Guid.NewGuid(), Guid.NewGuid().ToString(), "ValidateVaultPurpose",
            "partition-1", data);
    }
}
