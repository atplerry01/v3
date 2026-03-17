namespace Whycespace.Tests.Engines;

using Whycespace.Engines.T2E.Economic.Vault.Engines;
using Whycespace.Contracts.Engines;
using Xunit;

public sealed class VaultTransactionEngineTests
{
    private readonly VaultTransactionEngine _engine = new();

    private static Dictionary<string, object> ValidData() => new()
    {
        ["transactionId"] = Guid.NewGuid().ToString(),
        ["vaultId"] = Guid.NewGuid().ToString(),
        ["vaultAccountId"] = Guid.NewGuid().ToString(),
        ["initiatorIdentityId"] = Guid.NewGuid().ToString(),
        ["transactionType"] = "Contribution",
        ["amount"] = 5000m,
        ["currency"] = "GBP",
        ["description"] = "Capital contribution",
        ["referenceId"] = Guid.NewGuid().ToString(),
        ["referenceType"] = "SPV"
    };

    private static EngineContext CreateContext(Dictionary<string, object> data) =>
        new(Guid.NewGuid(), Guid.NewGuid().ToString(), "ExecuteTransaction", "partition-1", data);

    // --- ExecuteTransactionSuccessTest ---

    [Fact]
    public async Task ValidTransaction_ExecutesSuccessfully()
    {
        var result = await _engine.ExecuteAsync(CreateContext(ValidData()));

        Assert.True(result.Success);
        Assert.Equal("Completed", result.Output["status"]);
    }

    [Fact]
    public async Task ValidTransaction_EmitsLedgerEvent()
    {
        var result = await _engine.ExecuteAsync(CreateContext(ValidData()));

        Assert.True(result.Success);
        Assert.Contains(result.Events, e => e.EventType == "VaultLedgerEntryAppended");
    }

    [Fact]
    public async Task ValidTransaction_EmitsRegistryEvent()
    {
        var result = await _engine.ExecuteAsync(CreateContext(ValidData()));

        Assert.True(result.Success);
        Assert.Contains(result.Events, e => e.EventType == "VaultTransactionRegistered");
    }

    // --- TransactionValidationTest ---

    [Fact]
    public async Task InvalidAmount_Zero_Fails()
    {
        var data = ValidData();
        data["amount"] = 0m;

        var result = await _engine.ExecuteAsync(CreateContext(data));
        Assert.False(result.Success);
    }

    [Fact]
    public async Task InvalidAmount_Negative_Fails()
    {
        var data = ValidData();
        data["amount"] = -100m;

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
    public async Task MissingTransactionId_Fails()
    {
        var data = ValidData();
        data.Remove("transactionId");

        var result = await _engine.ExecuteAsync(CreateContext(data));
        Assert.False(result.Success);
    }

    [Fact]
    public async Task InvalidTransactionType_Fails()
    {
        var data = ValidData();
        data["transactionType"] = "InvalidType";

        var result = await _engine.ExecuteAsync(CreateContext(data));
        Assert.False(result.Success);
    }

    [Fact]
    public async Task UnsupportedCurrency_Fails()
    {
        var data = ValidData();
        data["currency"] = "BTC";

        var result = await _engine.ExecuteAsync(CreateContext(data));
        Assert.False(result.Success);
    }

    [Fact]
    public async Task MissingInitiatorIdentityId_Fails()
    {
        var data = ValidData();
        data.Remove("initiatorIdentityId");

        var result = await _engine.ExecuteAsync(CreateContext(data));
        Assert.False(result.Success);
    }

    // --- LedgerIntegrationTest ---

    [Fact]
    public async Task LedgerEntry_ContainsCorrectAmount()
    {
        var data = ValidData();
        data["amount"] = 2500m;

        var result = await _engine.ExecuteAsync(CreateContext(data));

        Assert.True(result.Success);
        var ledgerEvent = result.Events.First(e => e.EventType == "VaultLedgerEntryAppended");
        Assert.Equal(2500m, ledgerEvent.Payload["amount"]);
    }

    [Fact]
    public async Task Withdrawal_ProducesDebitDirection()
    {
        var data = ValidData();
        data["transactionType"] = "Withdrawal";

        var result = await _engine.ExecuteAsync(CreateContext(data));

        Assert.True(result.Success);
        var ledgerEvent = result.Events.First(e => e.EventType == "VaultLedgerEntryAppended");
        Assert.Equal("Debit", ledgerEvent.Payload["direction"]);
    }

    [Fact]
    public async Task Contribution_ProducesCreditDirection()
    {
        var data = ValidData();
        data["transactionType"] = "Contribution";

        var result = await _engine.ExecuteAsync(CreateContext(data));

        Assert.True(result.Success);
        var ledgerEvent = result.Events.First(e => e.EventType == "VaultLedgerEntryAppended");
        Assert.Equal("Credit", ledgerEvent.Payload["direction"]);
    }

    // --- RegistryIntegrationTest ---

    [Fact]
    public async Task RegistryEvent_ContainsTransactionMetadata()
    {
        var data = ValidData();
        var result = await _engine.ExecuteAsync(CreateContext(data));

        Assert.True(result.Success);
        var registryEvent = result.Events.First(e => e.EventType == "VaultTransactionRegistered");
        Assert.Equal(data["transactionId"], registryEvent.Payload["transactionId"]);
        Assert.Equal(data["vaultId"], registryEvent.Payload["vaultId"]);
        Assert.Equal(data["transactionType"], registryEvent.Payload["transactionType"]);
    }

    // --- TransactionLifecycleTest ---

    [Fact]
    public async Task LifecycleEvents_EmittedInOrder()
    {
        var result = await _engine.ExecuteAsync(CreateContext(ValidData()));

        Assert.True(result.Success);

        var eventTypes = result.Events.Select(e => e.EventType).ToList();
        Assert.Equal("VaultTransactionCreated", eventTypes[0]);
        Assert.Equal("VaultTransactionAuthorized", eventTypes[1]);
        Assert.Equal("VaultTransactionProcessing", eventTypes[2]);
        Assert.Equal("VaultLedgerEntryAppended", eventTypes[3]);
        Assert.Equal("VaultTransactionRegistered", eventTypes[4]);
        Assert.Equal("VaultTransactionCompleted", eventTypes[5]);
    }

    [Fact]
    public async Task AllEvents_TargetEconomicTopic()
    {
        var result = await _engine.ExecuteAsync(CreateContext(ValidData()));

        Assert.True(result.Success);
        Assert.All(result.Events, e =>
            Assert.Equal("whyce.economic.events", e.Payload["topic"]));
    }

    [Theory]
    [InlineData("Contribution")]
    [InlineData("Transfer")]
    [InlineData("Withdrawal")]
    [InlineData("Distribution")]
    [InlineData("Adjustment")]
    [InlineData("Refund")]
    public async Task AllTransactionTypes_ExecuteSuccessfully(string transactionType)
    {
        var data = ValidData();
        data["transactionType"] = transactionType;

        var result = await _engine.ExecuteAsync(CreateContext(data));
        Assert.True(result.Success);
    }
}
