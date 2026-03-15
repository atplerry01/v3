namespace Whycespace.Tests.Engines;

using Whycespace.Engines.T2E.Core.Vault;
using Whycespace.Contracts.Engines;
using Xunit;

public sealed class VaultSettlementEngineTests
{
    private readonly VaultSettlementEngine _engine = new();

    private static Dictionary<string, object> ValidData() => new()
    {
        ["settlementId"] = Guid.NewGuid().ToString(),
        ["transactionId"] = Guid.NewGuid().ToString(),
        ["vaultId"] = Guid.NewGuid().ToString(),
        ["vaultAccountId"] = Guid.NewGuid().ToString(),
        ["requestedBy"] = Guid.NewGuid().ToString(),
        ["amount"] = 5000m,
        ["currency"] = "GBP",
        ["transactionType"] = "Contribution",
        ["transactionStatus"] = "Completed",
        ["ledgerEntryExists"] = true,
        ["settlementReference"] = "REF-001",
        ["settlementScope"] = "Vault"
    };

    private static EngineContext CreateContext(Dictionary<string, object> data) =>
        new(Guid.NewGuid(), Guid.NewGuid().ToString(), "ExecuteSettlement", "partition-1", data);

    // --- SettlementSuccessTest ---

    [Fact]
    public async Task ValidSettlement_ExecutesSuccessfully()
    {
        var result = await _engine.ExecuteAsync(CreateContext(ValidData()));

        Assert.True(result.Success);
        Assert.Equal("Settled", result.Output["settlementStatus"]);
        Assert.Equal(true, result.Output["isSettled"]);
    }

    [Fact]
    public async Task ValidSettlement_EmitsRequestedAndCompletedEvents()
    {
        var result = await _engine.ExecuteAsync(CreateContext(ValidData()));

        Assert.True(result.Success);
        Assert.Contains(result.Events, e => e.EventType == "VaultSettlementRequested");
        Assert.Contains(result.Events, e => e.EventType == "VaultSettlementCompleted");
    }

    [Fact]
    public async Task AllEvents_TargetEconomicTopic()
    {
        var result = await _engine.ExecuteAsync(CreateContext(ValidData()));

        Assert.True(result.Success);
        Assert.All(result.Events, e =>
            Assert.Equal("whyce.economic.events", e.Payload["topic"]));
    }

    // --- TransactionExistenceTest ---

    [Fact]
    public async Task MissingTransactionId_Fails()
    {
        var data = ValidData();
        data.Remove("transactionId");

        var result = await _engine.ExecuteAsync(CreateContext(data));
        Assert.False(result.Success);
    }

    [Fact]
    public async Task InvalidTransactionId_Fails()
    {
        var data = ValidData();
        data["transactionId"] = "not-a-guid";

        var result = await _engine.ExecuteAsync(CreateContext(data));
        Assert.False(result.Success);
    }

    // --- LedgerVerificationTest ---

    [Fact]
    public async Task MissingLedgerEntries_Fails()
    {
        var data = ValidData();
        data["ledgerEntryExists"] = false;

        var result = await _engine.ExecuteAsync(CreateContext(data));
        Assert.False(result.Success);
        Assert.Contains("Ledger entries not found", (string)result.Output["error"]);
    }

    [Fact]
    public async Task LedgerEntryNotProvided_Fails()
    {
        var data = ValidData();
        data.Remove("ledgerEntryExists");

        var result = await _engine.ExecuteAsync(CreateContext(data));
        Assert.False(result.Success);
    }

    // --- DuplicateSettlementTest ---

    [Fact]
    public async Task AlreadySettledTransaction_Rejected()
    {
        var data = ValidData();
        data["transactionStatus"] = "Settled";

        var result = await _engine.ExecuteAsync(CreateContext(data));
        Assert.False(result.Success);
        Assert.Contains("already settled", (string)result.Output["error"]);
    }

    [Fact]
    public async Task PendingTransaction_CannotBeSettled()
    {
        var data = ValidData();
        data["transactionStatus"] = "Pending";

        var result = await _engine.ExecuteAsync(CreateContext(data));
        Assert.False(result.Success);
        Assert.Contains("cannot be settled", (string)result.Output["error"]);
    }

    // --- SettlementResultTest ---

    [Fact]
    public async Task SettlementResult_ContainsCorrectState()
    {
        var data = ValidData();
        var result = await _engine.ExecuteAsync(CreateContext(data));

        Assert.True(result.Success);
        Assert.Equal(data["settlementId"], result.Output["settlementId"]);
        Assert.Equal(data["transactionId"], result.Output["transactionId"]);
        Assert.Equal(data["vaultId"], result.Output["vaultId"]);
        Assert.Equal(5000m, result.Output["amount"]);
        Assert.Equal("GBP", result.Output["currency"]);
        Assert.Equal("Settled", result.Output["settlementStatus"]);
        Assert.Equal(true, result.Output["isSettled"]);
        Assert.True(result.Output.ContainsKey("settledAt"));
    }

    // --- Validation Tests ---

    [Fact]
    public async Task MissingSettlementId_Fails()
    {
        var data = ValidData();
        data.Remove("settlementId");

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
    public async Task MissingAmount_Fails()
    {
        var data = ValidData();
        data.Remove("amount");

        var result = await _engine.ExecuteAsync(CreateContext(data));
        Assert.False(result.Success);
    }

    [Fact]
    public async Task NegativeAmount_Fails()
    {
        var data = ValidData();
        data["amount"] = -100m;

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
    public async Task InvalidTransactionType_Fails()
    {
        var data = ValidData();
        data["transactionType"] = "InvalidType";

        var result = await _engine.ExecuteAsync(CreateContext(data));
        Assert.False(result.Success);
    }

    [Fact]
    public async Task CompletedEvent_ContainsSettlementMetadata()
    {
        var data = ValidData();
        var result = await _engine.ExecuteAsync(CreateContext(data));

        Assert.True(result.Success);
        var completedEvent = result.Events.First(e => e.EventType == "VaultSettlementCompleted");
        Assert.Equal(data["settlementId"], completedEvent.Payload["settlementId"]);
        Assert.Equal(data["transactionId"], completedEvent.Payload["transactionId"]);
        Assert.Equal("Settled", completedEvent.Payload["settlementStatus"]);
    }

    [Theory]
    [InlineData("Contribution")]
    [InlineData("Transfer")]
    [InlineData("Withdrawal")]
    [InlineData("Distribution")]
    [InlineData("Adjustment")]
    [InlineData("Refund")]
    public async Task AllTransactionTypes_SettleSuccessfully(string transactionType)
    {
        var data = ValidData();
        data["transactionType"] = transactionType;

        var result = await _engine.ExecuteAsync(CreateContext(data));
        Assert.True(result.Success);
    }
}
