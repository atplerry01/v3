namespace Whycespace.Tests.Engines;

using Whycespace.Engines.T2E.Economic.Vault.Accounting.Engines;
using Whycespace.Contracts.Engines;
using Xunit;

public sealed class VaultDoubleEntryAccountingEngineTests
{
    private readonly VaultDoubleEntryAccountingEngine _engine = new();

    private static List<Dictionary<string, object>> BalancedEntries(decimal amount = 5000m) =>
    [
        new()
        {
            ["accountId"] = Guid.NewGuid().ToString(),
            ["amount"] = amount,
            ["currency"] = "GBP",
            ["direction"] = "Debit"
        },
        new()
        {
            ["accountId"] = Guid.NewGuid().ToString(),
            ["amount"] = amount,
            ["currency"] = "GBP",
            ["direction"] = "Credit"
        }
    ];

    private static Dictionary<string, object> ValidData(List<Dictionary<string, object>>? entries = null) => new()
    {
        ["transactionId"] = Guid.NewGuid().ToString(),
        ["vaultId"] = Guid.NewGuid().ToString(),
        ["transactionType"] = "Contribution",
        ["ledgerEntries"] = entries ?? BalancedEntries()
    };

    private static EngineContext CreateContext(Dictionary<string, object> data) =>
        new(Guid.NewGuid(), Guid.NewGuid().ToString(), "ValidateDoubleEntry", "partition-1", data);

    // --- BalancedTransactionTest ---

    [Fact]
    public async Task BalancedTransaction_PassesValidation()
    {
        var result = await _engine.ExecuteAsync(CreateContext(ValidData()));

        Assert.True(result.Success);
        Assert.Equal("Passed", result.Output["validationStatus"]);
        Assert.Equal(true, result.Output["isBalanced"]);
    }

    [Fact]
    public async Task BalancedTransaction_EmitsPassedEvent()
    {
        var result = await _engine.ExecuteAsync(CreateContext(ValidData()));

        Assert.True(result.Success);
        Assert.Contains(result.Events, e => e.EventType == "VaultDoubleEntryValidationPassed");
    }

    [Fact]
    public async Task BalancedTransaction_EmitsRequestedEvent()
    {
        var result = await _engine.ExecuteAsync(CreateContext(ValidData()));

        Assert.True(result.Success);
        Assert.Contains(result.Events, e => e.EventType == "VaultDoubleEntryValidationRequested");
    }

    // --- ImbalancedTransactionTest ---

    [Fact]
    public async Task ImbalancedTransaction_FailsValidation()
    {
        var entries = new List<Dictionary<string, object>>
        {
            new()
            {
                ["accountId"] = Guid.NewGuid().ToString(),
                ["amount"] = 5000m,
                ["currency"] = "GBP",
                ["direction"] = "Debit"
            },
            new()
            {
                ["accountId"] = Guid.NewGuid().ToString(),
                ["amount"] = 3000m,
                ["currency"] = "GBP",
                ["direction"] = "Credit"
            }
        };

        var result = await _engine.ExecuteAsync(CreateContext(ValidData(entries)));

        Assert.False(result.Success);
        Assert.Contains("Imbalance detected", result.Output["error"] as string);
    }

    // --- DebitAggregationTest ---

    [Fact]
    public async Task MultipleDebits_AggregatedCorrectly()
    {
        var entries = new List<Dictionary<string, object>>
        {
            new() { ["accountId"] = Guid.NewGuid().ToString(), ["amount"] = 2000m, ["currency"] = "GBP", ["direction"] = "Debit" },
            new() { ["accountId"] = Guid.NewGuid().ToString(), ["amount"] = 3000m, ["currency"] = "GBP", ["direction"] = "Debit" },
            new() { ["accountId"] = Guid.NewGuid().ToString(), ["amount"] = 5000m, ["currency"] = "GBP", ["direction"] = "Credit" }
        };

        var result = await _engine.ExecuteAsync(CreateContext(ValidData(entries)));

        Assert.True(result.Success);
        Assert.Equal(5000m, result.Output["totalDebits"]);
    }

    // --- CreditAggregationTest ---

    [Fact]
    public async Task MultipleCredits_AggregatedCorrectly()
    {
        var entries = new List<Dictionary<string, object>>
        {
            new() { ["accountId"] = Guid.NewGuid().ToString(), ["amount"] = 5000m, ["currency"] = "GBP", ["direction"] = "Debit" },
            new() { ["accountId"] = Guid.NewGuid().ToString(), ["amount"] = 2000m, ["currency"] = "GBP", ["direction"] = "Credit" },
            new() { ["accountId"] = Guid.NewGuid().ToString(), ["amount"] = 3000m, ["currency"] = "GBP", ["direction"] = "Credit" }
        };

        var result = await _engine.ExecuteAsync(CreateContext(ValidData(entries)));

        Assert.True(result.Success);
        Assert.Equal(5000m, result.Output["totalCredits"]);
    }

    // --- ValidationResultTest ---

    [Fact]
    public async Task ValidationResult_ReturnsCorrectTotals()
    {
        var entries = BalancedEntries(7500m);
        var result = await _engine.ExecuteAsync(CreateContext(ValidData(entries)));

        Assert.True(result.Success);
        Assert.Equal(7500m, result.Output["totalDebits"]);
        Assert.Equal(7500m, result.Output["totalCredits"]);
        Assert.Equal(true, result.Output["isBalanced"]);
        Assert.Equal("Passed", result.Output["validationStatus"]);
    }

    // --- Input validation tests ---

    [Fact]
    public async Task MissingTransactionId_Fails()
    {
        var data = ValidData();
        data.Remove("transactionId");

        var result = await _engine.ExecuteAsync(CreateContext(data));
        Assert.False(result.Success);
    }

    [Fact]
    public async Task MissingVaultId_Fails()
    {
        var data = ValidData();
        data.Remove("vaultId");

        var result = await _engine.ExecuteAsync(CreateContext(data));
        Assert.False(result.Success);
    }

    [Fact]
    public async Task MissingTransactionType_Fails()
    {
        var data = ValidData();
        data.Remove("transactionType");

        var result = await _engine.ExecuteAsync(CreateContext(data));
        Assert.False(result.Success);
    }

    [Fact]
    public async Task MissingLedgerEntries_Fails()
    {
        var data = ValidData();
        data.Remove("ledgerEntries");

        var result = await _engine.ExecuteAsync(CreateContext(data));
        Assert.False(result.Success);
    }

    [Fact]
    public async Task InvalidDirection_Fails()
    {
        var entries = new List<Dictionary<string, object>>
        {
            new() { ["accountId"] = Guid.NewGuid().ToString(), ["amount"] = 1000m, ["currency"] = "GBP", ["direction"] = "Invalid" }
        };

        var result = await _engine.ExecuteAsync(CreateContext(ValidData(entries)));
        Assert.False(result.Success);
    }

    [Fact]
    public async Task ZeroAmount_Fails()
    {
        var entries = new List<Dictionary<string, object>>
        {
            new() { ["accountId"] = Guid.NewGuid().ToString(), ["amount"] = 0m, ["currency"] = "GBP", ["direction"] = "Debit" }
        };

        var result = await _engine.ExecuteAsync(CreateContext(ValidData(entries)));
        Assert.False(result.Success);
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
}
