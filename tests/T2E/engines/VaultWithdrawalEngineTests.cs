namespace Whycespace.Tests.Engines;

using Whycespace.Engines.T2E.Economic.Vault.Withdrawal.Engines;
using Whycespace.Contracts.Engines;
using Xunit;

public sealed class VaultWithdrawalEngineTests
{
    private readonly VaultWithdrawalEngine _engine = new();

    private static EngineContext CreateContext(Dictionary<string, object> data) =>
        new(Guid.NewGuid(), Guid.NewGuid().ToString(), "ExecuteWithdrawal", "partition-1", data);

    private static Dictionary<string, object> ValidWithdrawalData(decimal amount = 500m, decimal availableBalance = 10000m) =>
        new()
        {
            ["withdrawalId"] = Guid.NewGuid().ToString(),
            ["vaultId"] = Guid.NewGuid().ToString(),
            ["vaultAccountId"] = Guid.NewGuid().ToString(),
            ["initiatorIdentityId"] = Guid.NewGuid().ToString(),
            ["amount"] = amount,
            ["currency"] = "GBP",
            ["withdrawalDestination"] = "ExternalBank",
            ["availableBalance"] = availableBalance,
            ["description"] = "Operational payout"
        };

    [Fact]
    public async Task ExecuteWithdrawal_ValidRequest_Succeeds()
    {
        var context = CreateContext(ValidWithdrawalData());

        var result = await _engine.ExecuteAsync(context);

        Assert.True(result.Success);
        Assert.True(result.Output.ContainsKey("withdrawalId"));
        Assert.True(result.Output.ContainsKey("transactionId"));
        Assert.Equal("Completed", result.Output["transactionStatus"]);
    }

    [Fact]
    public async Task ExecuteWithdrawal_EmitsCorrectEvents()
    {
        var context = CreateContext(ValidWithdrawalData());

        var result = await _engine.ExecuteAsync(context);

        Assert.True(result.Success);
        Assert.Equal(3, result.Events.Count);
        Assert.Contains(result.Events, e => e.EventType == "VaultWithdrawalRequested");
        Assert.Contains(result.Events, e => e.EventType == "VaultWithdrawalProcessing");
        Assert.Contains(result.Events, e => e.EventType == "VaultWithdrawalCompleted");
    }

    [Fact]
    public async Task ExecuteWithdrawal_LedgerDebitEntry_Appended()
    {
        var context = CreateContext(ValidWithdrawalData());

        var result = await _engine.ExecuteAsync(context);

        Assert.True(result.Success);
        var processingEvent = Assert.Single(result.Events, e => e.EventType == "VaultWithdrawalProcessing");
        Assert.Equal("Debit", processingEvent.Payload["ledgerDirection"]);
        Assert.Equal("Withdrawal", processingEvent.Payload["ledgerTransactionType"]);
    }

    [Fact]
    public async Task ExecuteWithdrawal_RegistryUpdated()
    {
        var context = CreateContext(ValidWithdrawalData());

        var result = await _engine.ExecuteAsync(context);

        Assert.True(result.Success);
        var completedEvent = Assert.Single(result.Events, e => e.EventType == "VaultWithdrawalCompleted");
        Assert.Equal("Completed", completedEvent.Payload["transactionStatus"]);
        Assert.True(completedEvent.Payload.ContainsKey("transactionId"));
    }

    [Fact]
    public async Task ExecuteWithdrawal_InsufficientFunds_Fails()
    {
        var data = ValidWithdrawalData(amount: 15000m, availableBalance: 10000m);
        var context = CreateContext(data);

        var result = await _engine.ExecuteAsync(context);

        Assert.False(result.Success);
    }

    [Fact]
    public async Task ExecuteWithdrawal_ZeroAmount_Fails()
    {
        var data = ValidWithdrawalData(amount: 0m);
        var context = CreateContext(data);

        var result = await _engine.ExecuteAsync(context);

        Assert.False(result.Success);
    }

    [Fact]
    public async Task ExecuteWithdrawal_NegativeAmount_Fails()
    {
        var data = ValidWithdrawalData(amount: -100m);
        var context = CreateContext(data);

        var result = await _engine.ExecuteAsync(context);

        Assert.False(result.Success);
    }

    [Fact]
    public async Task ExecuteWithdrawal_MissingVaultId_Fails()
    {
        var data = ValidWithdrawalData();
        data.Remove("vaultId");
        var context = CreateContext(data);

        var result = await _engine.ExecuteAsync(context);

        Assert.False(result.Success);
    }

    [Fact]
    public async Task ExecuteWithdrawal_MissingCurrency_Fails()
    {
        var data = ValidWithdrawalData();
        data.Remove("currency");
        var context = CreateContext(data);

        var result = await _engine.ExecuteAsync(context);

        Assert.False(result.Success);
    }

    [Fact]
    public async Task ExecuteWithdrawal_InvalidDestination_Fails()
    {
        var data = ValidWithdrawalData();
        data["withdrawalDestination"] = "InvalidDestination";
        var context = CreateContext(data);

        var result = await _engine.ExecuteAsync(context);

        Assert.False(result.Success);
    }

    [Fact]
    public async Task ExecuteWithdrawal_MissingWithdrawalId_Fails()
    {
        var data = ValidWithdrawalData();
        data.Remove("withdrawalId");
        var context = CreateContext(data);

        var result = await _engine.ExecuteAsync(context);

        Assert.False(result.Success);
    }

    [Fact]
    public async Task ExecuteWithdrawal_UnsupportedCurrency_Fails()
    {
        var data = ValidWithdrawalData();
        data["currency"] = "BTC";
        var context = CreateContext(data);

        var result = await _engine.ExecuteAsync(context);

        Assert.False(result.Success);
    }

    [Fact]
    public async Task ExecuteWithdrawal_OptionalReferenceFields_IncludedInEvent()
    {
        var data = ValidWithdrawalData();
        data["referenceId"] = "REF-001";
        data["referenceType"] = "InvoicePayment";
        var context = CreateContext(data);

        var result = await _engine.ExecuteAsync(context);

        Assert.True(result.Success);
        var completedEvent = Assert.Single(result.Events, e => e.EventType == "VaultWithdrawalCompleted");
        Assert.Equal("REF-001", completedEvent.Payload["referenceId"]);
        Assert.Equal("InvoicePayment", completedEvent.Payload["referenceType"]);
    }
}
