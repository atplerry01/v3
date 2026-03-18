namespace Whycespace.Tests.Engines;

using Whycespace.Engines.T2E.Economic.Vault.Validation.Engines;
using Whycespace.Contracts.Engines;
using Xunit;

public sealed class VaultTransactionValidationEngineTests
{
    private readonly VaultTransactionValidationEngine _engine = new();

    private static Dictionary<string, object> ValidData() => new()
    {
        ["transactionId"] = Guid.NewGuid().ToString(),
        ["vaultId"] = Guid.NewGuid().ToString(),
        ["vaultAccountId"] = Guid.NewGuid().ToString(),
        ["initiatorIdentityId"] = Guid.NewGuid().ToString(),
        ["transactionType"] = "Contribution",
        ["amount"] = 5000m,
        ["currency"] = "GBP"
    };

    private static EngineContext CreateContext(Dictionary<string, object> data) =>
        new(Guid.NewGuid(), Guid.NewGuid().ToString(), "ValidateTransaction", "partition-1", data);

    // --- ValidTransactionTest ---

    [Fact]
    public async Task ValidTransaction_PassesValidation()
    {
        var result = await _engine.ExecuteAsync(CreateContext(ValidData()));

        Assert.True(result.Success);
        Assert.Equal(true, result.Output["isValid"]);
        Assert.Equal("Passed", result.Output["validationStatus"]);
    }

    [Fact]
    public async Task ValidTransaction_EmitsRequestedAndPassedEvents()
    {
        var result = await _engine.ExecuteAsync(CreateContext(ValidData()));

        Assert.True(result.Success);
        Assert.Equal(2, result.Events.Count);
        Assert.Equal("VaultTransactionValidationRequested", result.Events[0].EventType);
        Assert.Equal("VaultTransactionValidationPassed", result.Events[1].EventType);
    }

    // --- InvalidAmountTest ---

    [Fact]
    public async Task NegativeAmount_Rejected()
    {
        var data = ValidData();
        data["amount"] = -100m;

        var result = await _engine.ExecuteAsync(CreateContext(data));

        Assert.False(result.Success);
        Assert.Contains("greater than zero", result.Output["error"] as string);
    }

    [Fact]
    public async Task ZeroAmount_Rejected()
    {
        var data = ValidData();
        data["amount"] = 0m;

        var result = await _engine.ExecuteAsync(CreateContext(data));

        Assert.False(result.Success);
        Assert.Contains("greater than zero", result.Output["error"] as string);
    }

    // --- MissingVaultTest ---

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

    // --- UnsupportedTransactionTypeTest ---

    [Fact]
    public async Task UnsupportedTransactionType_Rejected()
    {
        var data = ValidData();
        data["transactionType"] = "InvalidType";

        var result = await _engine.ExecuteAsync(CreateContext(data));

        Assert.False(result.Success);
        Assert.Contains("Unsupported transactionType", result.Output["error"] as string);
    }

    [Fact]
    public async Task MissingTransactionType_Rejected()
    {
        var data = ValidData();
        data.Remove("transactionType");

        var result = await _engine.ExecuteAsync(CreateContext(data));

        Assert.False(result.Success);
    }

    // --- ValidationResultTest ---

    [Fact]
    public async Task ValidationResult_ContainsCorrectReason()
    {
        var result = await _engine.ExecuteAsync(CreateContext(ValidData()));

        Assert.True(result.Success);
        Assert.Equal("All validation rules passed", result.Output["validationReason"]);
    }

    [Fact]
    public async Task FailedValidation_ContainsErrorReason()
    {
        var data = ValidData();
        data.Remove("currency");

        var result = await _engine.ExecuteAsync(CreateContext(data));

        Assert.False(result.Success);
        Assert.Contains("currency", (result.Output["error"] as string)!, StringComparison.OrdinalIgnoreCase);
    }

    // --- Additional coverage ---

    [Fact]
    public async Task MissingTransactionId_Rejected()
    {
        var data = ValidData();
        data.Remove("transactionId");

        var result = await _engine.ExecuteAsync(CreateContext(data));
        Assert.False(result.Success);
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

    [Theory]
    [InlineData("Contribution")]
    [InlineData("Transfer")]
    [InlineData("Withdrawal")]
    [InlineData("Distribution")]
    [InlineData("Adjustment")]
    [InlineData("Refund")]
    public async Task AllSupportedTransactionTypes_PassValidation(string transactionType)
    {
        var data = ValidData();
        data["transactionType"] = transactionType;

        var result = await _engine.ExecuteAsync(CreateContext(data));
        Assert.True(result.Success);
    }

    [Fact]
    public async Task AllEvents_TargetEconomicTopic()
    {
        var result = await _engine.ExecuteAsync(CreateContext(ValidData()));

        Assert.True(result.Success);
        Assert.All(result.Events, e =>
            Assert.Equal("whyce.economic.events", e.Payload["topic"]));
    }
}
