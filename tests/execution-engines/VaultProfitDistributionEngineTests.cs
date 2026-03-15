namespace Whycespace.Tests.ExecutionEngines;

using Whycespace.Engines.T2E.Core.Vault;
using Whycespace.Contracts.Engines;
using Xunit;

public sealed class VaultProfitDistributionEngineTests
{
    private readonly VaultProfitDistributionEngine _engine = new();

    private static Dictionary<string, object> CreateValidData(
        decimal totalProfitAmount = 100_000m,
        decimal? vaultBalance = 200_000m,
        List<Dictionary<string, object>>? allocations = null)
    {
        var participantA = Guid.NewGuid().ToString();
        var participantB = Guid.NewGuid().ToString();
        var participantC = Guid.NewGuid().ToString();

        allocations ??= new List<Dictionary<string, object>>
        {
            new() { ["recipientId"] = participantA, ["percentage"] = 50m, ["status"] = "Active" },
            new() { ["recipientId"] = participantB, ["percentage"] = 30m, ["status"] = "Active" },
            new() { ["recipientId"] = participantC, ["percentage"] = 20m, ["status"] = "Active" }
        };

        var data = new Dictionary<string, object>
        {
            ["distributionId"] = Guid.NewGuid().ToString(),
            ["vaultId"] = Guid.NewGuid().ToString(),
            ["vaultAccountId"] = Guid.NewGuid().ToString(),
            ["totalProfitAmount"] = totalProfitAmount,
            ["currency"] = "GBP",
            ["initiatorIdentityId"] = Guid.NewGuid().ToString(),
            ["distributionReference"] = "DIST-2026-001",
            ["description"] = "Q1 profit distribution",
            ["allocations"] = allocations
        };

        if (vaultBalance.HasValue)
            data["vaultBalance"] = vaultBalance.Value;

        return data;
    }

    private EngineContext CreateContext(Dictionary<string, object> data)
    {
        return new EngineContext(
            Guid.NewGuid(), Guid.NewGuid().ToString(), "DistributeProfit",
            "partition-1", data);
    }

    // --- ExecuteDistributionSuccessTest ---

    [Fact]
    public async Task ExecuteDistribution_ValidData_Succeeds()
    {
        var data = CreateValidData();
        var result = await _engine.ExecuteAsync(CreateContext(data));

        Assert.True(result.Success);
        Assert.Equal("Completed", result.Output["distributionStatus"]);
        Assert.Equal(3, result.Output["participantCount"]);
        Assert.Equal(100_000m, result.Output["totalProfitAmount"]);
    }

    [Fact]
    public async Task ExecuteDistribution_CreatesLedgerEntries()
    {
        var data = CreateValidData();
        var result = await _engine.ExecuteAsync(CreateContext(data));

        Assert.True(result.Success);
        var ledgerEvents = result.Events.Where(e => e.EventType == "VaultLedgerEntryAppended").ToList();
        // 1 debit + 3 credits = 4 ledger entries
        Assert.Equal(4, ledgerEvents.Count);
    }

    [Fact]
    public async Task ExecuteDistribution_RegistersTransaction()
    {
        var data = CreateValidData();
        var result = await _engine.ExecuteAsync(CreateContext(data));

        Assert.True(result.Success);
        Assert.Contains(result.Events, e => e.EventType == "VaultTransactionRegistered");
    }

    [Fact]
    public async Task ExecuteDistribution_EmitsFullLifecycleEvents()
    {
        var data = CreateValidData();
        var result = await _engine.ExecuteAsync(CreateContext(data));

        Assert.True(result.Success);
        Assert.Contains(result.Events, e => e.EventType == "VaultProfitDistributionInitiated");
        Assert.Contains(result.Events, e => e.EventType == "VaultProfitDistributionCalculated");
        Assert.Contains(result.Events, e => e.EventType == "VaultProfitDistributionCompleted");
    }

    // --- AllocationCalculationTest ---

    [Fact]
    public async Task AllocationCalculation_DistributesCorrectAmounts()
    {
        var participantA = Guid.NewGuid().ToString();
        var participantB = Guid.NewGuid().ToString();
        var participantC = Guid.NewGuid().ToString();

        var allocations = new List<Dictionary<string, object>>
        {
            new() { ["recipientId"] = participantA, ["percentage"] = 50m },
            new() { ["recipientId"] = participantB, ["percentage"] = 30m },
            new() { ["recipientId"] = participantC, ["percentage"] = 20m }
        };

        var data = CreateValidData(totalProfitAmount: 100_000m, allocations: allocations);
        var result = await _engine.ExecuteAsync(CreateContext(data));

        Assert.True(result.Success);

        // Check credit ledger entries for correct amounts
        var creditEvents = result.Events
            .Where(e => e.EventType == "VaultLedgerEntryAppended"
                && e.Payload.GetValueOrDefault("direction") as string == "Credit")
            .ToList();

        Assert.Equal(3, creditEvents.Count);

        var amounts = creditEvents.Select(e => (decimal)e.Payload["amount"]).OrderByDescending(a => a).ToList();
        Assert.Equal(50_000m, amounts[0]);
        Assert.Equal(30_000m, amounts[1]);
        Assert.Equal(20_000m, amounts[2]);

        // Verify total distributed equals total profit
        Assert.Equal(100_000m, amounts.Sum());
    }

    [Fact]
    public async Task AllocationCalculation_TwoParticipants_DistributesCorrectly()
    {
        var participantA = Guid.NewGuid().ToString();
        var participantB = Guid.NewGuid().ToString();

        var allocations = new List<Dictionary<string, object>>
        {
            new() { ["recipientId"] = participantA, ["percentage"] = 70m },
            new() { ["recipientId"] = participantB, ["percentage"] = 30m }
        };

        var data = CreateValidData(totalProfitAmount: 10_000m, allocations: allocations);
        var result = await _engine.ExecuteAsync(CreateContext(data));

        Assert.True(result.Success);
        Assert.Equal(2, result.Output["participantCount"]);
    }

    // --- DistributionValidationTest ---

    [Fact]
    public async Task Validation_MissingDistributionId_Fails()
    {
        var data = CreateValidData();
        data.Remove("distributionId");

        var result = await _engine.ExecuteAsync(CreateContext(data));
        Assert.False(result.Success);
    }

    [Fact]
    public async Task Validation_MissingVaultId_Fails()
    {
        var data = CreateValidData();
        data.Remove("vaultId");

        var result = await _engine.ExecuteAsync(CreateContext(data));
        Assert.False(result.Success);
    }

    [Fact]
    public async Task Validation_ZeroAmount_Fails()
    {
        var data = CreateValidData(totalProfitAmount: 0m);

        var result = await _engine.ExecuteAsync(CreateContext(data));
        Assert.False(result.Success);
    }

    [Fact]
    public async Task Validation_NegativeAmount_Fails()
    {
        var data = CreateValidData(totalProfitAmount: -500m);

        var result = await _engine.ExecuteAsync(CreateContext(data));
        Assert.False(result.Success);
    }

    [Fact]
    public async Task Validation_UnsupportedCurrency_Fails()
    {
        var data = CreateValidData();
        data["currency"] = "BTC";

        var result = await _engine.ExecuteAsync(CreateContext(data));
        Assert.False(result.Success);
    }

    [Fact]
    public async Task Validation_InsufficientBalance_Fails()
    {
        var data = CreateValidData(totalProfitAmount: 100_000m, vaultBalance: 50_000m);

        var result = await _engine.ExecuteAsync(CreateContext(data));
        Assert.False(result.Success);
    }

    [Fact]
    public async Task Validation_EmptyAllocations_Fails()
    {
        var data = CreateValidData(allocations: new List<Dictionary<string, object>>());

        var result = await _engine.ExecuteAsync(CreateContext(data));
        Assert.False(result.Success);
    }

    [Fact]
    public async Task Validation_AllocationsExceed100Percent_Fails()
    {
        var allocations = new List<Dictionary<string, object>>
        {
            new() { ["recipientId"] = Guid.NewGuid().ToString(), ["percentage"] = 60m },
            new() { ["recipientId"] = Guid.NewGuid().ToString(), ["percentage"] = 50m }
        };

        var data = CreateValidData(allocations: allocations);

        var result = await _engine.ExecuteAsync(CreateContext(data));
        Assert.False(result.Success);
    }

    [Fact]
    public async Task Validation_InvalidRecipientId_Fails()
    {
        var allocations = new List<Dictionary<string, object>>
        {
            new() { ["recipientId"] = Guid.Empty.ToString(), ["percentage"] = 100m }
        };

        var data = CreateValidData(allocations: allocations);

        var result = await _engine.ExecuteAsync(CreateContext(data));
        Assert.False(result.Success);
    }

    // --- LedgerIntegrationTest ---

    [Fact]
    public async Task Ledger_DebitEntryHasCorrectDirection()
    {
        var data = CreateValidData();
        var result = await _engine.ExecuteAsync(CreateContext(data));

        Assert.True(result.Success);

        var debitEntry = result.Events
            .Where(e => e.EventType == "VaultLedgerEntryAppended"
                && e.Payload.GetValueOrDefault("direction") as string == "Debit")
            .ToList();

        Assert.Single(debitEntry);
        Assert.Equal(100_000m, debitEntry[0].Payload["amount"]);
        Assert.Equal("Distribution", debitEntry[0].Payload["transactionType"]);
    }

    [Fact]
    public async Task Ledger_CreditEntriesMatchParticipantCount()
    {
        var data = CreateValidData();
        var result = await _engine.ExecuteAsync(CreateContext(data));

        Assert.True(result.Success);

        var creditEntries = result.Events
            .Where(e => e.EventType == "VaultLedgerEntryAppended"
                && e.Payload.GetValueOrDefault("direction") as string == "Credit")
            .ToList();

        Assert.Equal(3, creditEntries.Count);
    }

    [Fact]
    public async Task Ledger_AllEntriesHaveDistributionType()
    {
        var data = CreateValidData();
        var result = await _engine.ExecuteAsync(CreateContext(data));

        Assert.True(result.Success);

        var ledgerEvents = result.Events
            .Where(e => e.EventType == "VaultLedgerEntryAppended")
            .ToList();

        Assert.All(ledgerEvents, e => Assert.Equal("Distribution", e.Payload["transactionType"]));
    }

    // --- RegistryIntegrationTest ---

    [Fact]
    public async Task Registry_TransactionRegistered_ContainsCorrectData()
    {
        var data = CreateValidData();
        var result = await _engine.ExecuteAsync(CreateContext(data));

        Assert.True(result.Success);

        var registryEvent = result.Events.First(e => e.EventType == "VaultTransactionRegistered");
        Assert.Equal("Distribution", registryEvent.Payload["transactionType"]);
        Assert.Equal(100_000m, registryEvent.Payload["amount"]);
        Assert.Equal("GBP", registryEvent.Payload["currency"]);
        Assert.Equal(3, registryEvent.Payload["participantCount"]);
    }

    [Fact]
    public async Task Registry_EventsHaveKafkaTopic()
    {
        var data = CreateValidData();
        var result = await _engine.ExecuteAsync(CreateContext(data));

        Assert.True(result.Success);
        Assert.All(result.Events, e => Assert.Equal("whyce.economic.events", e.Payload["topic"]));
    }

    // --- No vault balance provided (skip balance check) ---

    [Fact]
    public async Task Distribution_WithoutBalanceProvided_Succeeds()
    {
        var data = CreateValidData(vaultBalance: null);

        var result = await _engine.ExecuteAsync(CreateContext(data));
        Assert.True(result.Success);
    }
}
