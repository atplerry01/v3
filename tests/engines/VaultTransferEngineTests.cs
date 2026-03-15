namespace Whycespace.Tests.Engines;

using Whycespace.Engines.T2E.Core.Vault;
using Whycespace.Contracts.Engines;
using Xunit;

public sealed class VaultTransferEngineTests
{
    private readonly VaultTransferEngine _engine = new();

    private static EngineContext CreateContext(Dictionary<string, object> data)
    {
        return new EngineContext(
            Guid.NewGuid(), Guid.NewGuid().ToString(), "ExecuteTransfer",
            "partition-1", data);
    }

    private static Dictionary<string, object> ValidTransferData(
        decimal amount = 500m,
        decimal sourceBalance = 1000m)
    {
        return new Dictionary<string, object>
        {
            ["transferId"] = Guid.NewGuid().ToString(),
            ["sourceVaultId"] = Guid.NewGuid().ToString(),
            ["sourceVaultAccountId"] = Guid.NewGuid().ToString(),
            ["destinationVaultId"] = Guid.NewGuid().ToString(),
            ["destinationVaultAccountId"] = Guid.NewGuid().ToString(),
            ["initiatorIdentityId"] = Guid.NewGuid().ToString(),
            ["amount"] = amount,
            ["currency"] = "GBP",
            ["sourceBalance"] = sourceBalance,
            ["description"] = "Test transfer"
        };
    }

    [Fact]
    public async Task ExecuteTransfer_ValidCommand_Succeeds()
    {
        var data = ValidTransferData();
        var result = await _engine.ExecuteAsync(CreateContext(data));

        Assert.True(result.Success);
        Assert.True(result.Output.ContainsKey("transferId"));
        Assert.True(result.Output.ContainsKey("transactionId"));
        Assert.Equal("Completed", result.Output["transactionStatus"]);
    }

    [Fact]
    public async Task ExecuteTransfer_ValidCommand_CreatesDebitAndCreditLedgerEntries()
    {
        var data = ValidTransferData();
        var result = await _engine.ExecuteAsync(CreateContext(data));

        Assert.True(result.Success);
        Assert.Equal(4, result.Events.Count);

        var debitEvent = result.Events[1];
        Assert.Equal("VaultTransferProcessing", debitEvent.EventType);
        Assert.Equal("Debit", debitEvent.Payload["ledgerDirection"]);
        Assert.Equal("TransferOut", debitEvent.Payload["ledgerTransactionType"]);

        var creditEvent = result.Events[2];
        Assert.Equal("VaultTransferProcessing", creditEvent.EventType);
        Assert.Equal("Credit", creditEvent.Payload["ledgerDirection"]);
        Assert.Equal("TransferIn", creditEvent.Payload["ledgerTransactionType"]);
    }

    [Fact]
    public async Task ExecuteTransfer_ValidCommand_DebitAndCreditAmountsMatch()
    {
        var data = ValidTransferData(amount: 250m);
        var result = await _engine.ExecuteAsync(CreateContext(data));

        Assert.True(result.Success);

        var debitEvent = result.Events[1];
        var creditEvent = result.Events[2];

        Assert.Equal(250m, debitEvent.Payload["amount"]);
        Assert.Equal(250m, creditEvent.Payload["amount"]);
    }

    [Fact]
    public async Task ExecuteTransfer_ValidCommand_RegistersTransaction()
    {
        var data = ValidTransferData();
        var result = await _engine.ExecuteAsync(CreateContext(data));

        Assert.True(result.Success);
        var completedEvent = result.Events[3];
        Assert.Equal("VaultTransferCompleted", completedEvent.EventType);
        Assert.Equal("Completed", completedEvent.Payload["transactionStatus"]);
    }

    [Fact]
    public async Task ExecuteTransfer_InsufficientFunds_Fails()
    {
        var data = ValidTransferData(amount: 2000m, sourceBalance: 500m);
        var result = await _engine.ExecuteAsync(CreateContext(data));

        Assert.False(result.Success);
    }

    [Fact]
    public async Task ExecuteTransfer_ZeroAmount_Fails()
    {
        var data = ValidTransferData(amount: 0m);
        var result = await _engine.ExecuteAsync(CreateContext(data));

        Assert.False(result.Success);
    }

    [Fact]
    public async Task ExecuteTransfer_NegativeAmount_Fails()
    {
        var data = ValidTransferData(amount: -100m);
        var result = await _engine.ExecuteAsync(CreateContext(data));

        Assert.False(result.Success);
    }

    [Fact]
    public async Task ExecuteTransfer_MissingTransferId_Fails()
    {
        var data = ValidTransferData();
        data.Remove("transferId");
        var result = await _engine.ExecuteAsync(CreateContext(data));

        Assert.False(result.Success);
    }

    [Fact]
    public async Task ExecuteTransfer_MissingSourceVaultId_Fails()
    {
        var data = ValidTransferData();
        data.Remove("sourceVaultId");
        var result = await _engine.ExecuteAsync(CreateContext(data));

        Assert.False(result.Success);
    }

    [Fact]
    public async Task ExecuteTransfer_MissingDestinationVaultAccountId_Fails()
    {
        var data = ValidTransferData();
        data.Remove("destinationVaultAccountId");
        var result = await _engine.ExecuteAsync(CreateContext(data));

        Assert.False(result.Success);
    }

    [Fact]
    public async Task ExecuteTransfer_MissingCurrency_Fails()
    {
        var data = ValidTransferData();
        data.Remove("currency");
        var result = await _engine.ExecuteAsync(CreateContext(data));

        Assert.False(result.Success);
    }

    [Fact]
    public async Task ExecuteTransfer_UnsupportedCurrency_Fails()
    {
        var data = ValidTransferData();
        data["currency"] = "BTC";
        var result = await _engine.ExecuteAsync(CreateContext(data));

        Assert.False(result.Success);
    }

    [Fact]
    public async Task ExecuteTransfer_SameSourceAndDestinationAccount_Fails()
    {
        var data = ValidTransferData();
        var sameVaultId = Guid.NewGuid().ToString();
        var sameAccountId = Guid.NewGuid().ToString();
        data["sourceVaultId"] = sameVaultId;
        data["destinationVaultId"] = sameVaultId;
        data["sourceVaultAccountId"] = sameAccountId;
        data["destinationVaultAccountId"] = sameAccountId;

        var result = await _engine.ExecuteAsync(CreateContext(data));

        Assert.False(result.Success);
    }

    [Fact]
    public async Task ExecuteTransfer_EmitsAllFourLifecycleEvents()
    {
        var data = ValidTransferData();
        var result = await _engine.ExecuteAsync(CreateContext(data));

        Assert.True(result.Success);
        Assert.Equal(4, result.Events.Count);
        Assert.Equal("VaultTransferInitiated", result.Events[0].EventType);
        Assert.Equal("VaultTransferProcessing", result.Events[1].EventType);
        Assert.Equal("VaultTransferProcessing", result.Events[2].EventType);
        Assert.Equal("VaultTransferCompleted", result.Events[3].EventType);
    }

    [Fact]
    public async Task ExecuteTransfer_AllEventsRouteToEconomicTopic()
    {
        var data = ValidTransferData();
        var result = await _engine.ExecuteAsync(CreateContext(data));

        Assert.True(result.Success);
        foreach (var evt in result.Events)
        {
            Assert.Equal("whyce.economic.events", evt.Payload["topic"]);
        }
    }

    [Fact]
    public async Task ExecuteTransfer_OptionalReferenceFieldsIncludedInCompletedEvent()
    {
        var data = ValidTransferData();
        data["referenceId"] = "REF-001";
        data["referenceType"] = "TreasuryAllocation";

        var result = await _engine.ExecuteAsync(CreateContext(data));

        Assert.True(result.Success);
        var completedEvent = result.Events[3];
        Assert.Equal("REF-001", completedEvent.Payload["referenceId"]);
        Assert.Equal("TreasuryAllocation", completedEvent.Payload["referenceType"]);
    }
}