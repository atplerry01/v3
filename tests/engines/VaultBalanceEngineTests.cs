namespace Whycespace.Tests.Engines;

using Whycespace.Engines.T2E.Economic.Vault.Engines;
using Whycespace.Contracts.Engines;
using Xunit;

public sealed class VaultBalanceEngineTests
{
    private readonly VaultBalanceEngine _engine = new();

    private static readonly string TestVaultId = Guid.NewGuid().ToString();
    private static readonly string TestAccountId = Guid.NewGuid().ToString();

    private static EngineContext CreateContext(Dictionary<string, object> data) =>
        new(Guid.NewGuid(), Guid.NewGuid().ToString(), "ComputeVaultBalance", "partition-1", data);

    private static List<IReadOnlyDictionary<string, object>> CreateLedgerEntries(params (string direction, decimal amount)[] entries)
    {
        var list = new List<IReadOnlyDictionary<string, object>>();
        foreach (var (direction, amount) in entries)
        {
            list.Add(new Dictionary<string, object>
            {
                ["vaultAccountId"] = TestAccountId,
                ["currency"] = "GBP",
                ["amount"] = amount,
                ["direction"] = direction
            });
        }
        return list;
    }

    private static Dictionary<string, object> ValidBalanceData(List<IReadOnlyDictionary<string, object>>? ledgerEntries = null) =>
        new()
        {
            ["vaultId"] = TestVaultId,
            ["vaultAccountId"] = TestAccountId,
            ["currency"] = "GBP",
            ["ledgerEntries"] = ledgerEntries ?? CreateLedgerEntries(
                ("Credit", 50000m),
                ("Credit", 20000m),
                ("Debit", 10000m)
            )
        };

    [Fact]
    public async Task ComputeBalance_ValidRequest_Succeeds()
    {
        var context = CreateContext(ValidBalanceData());

        var result = await _engine.ExecuteAsync(context);

        Assert.True(result.Success);
        Assert.Equal(60000m, result.Output["currentBalance"]);
        Assert.Equal(70000m, result.Output["totalCredits"]);
        Assert.Equal(10000m, result.Output["totalDebits"]);
        Assert.Equal("GBP", result.Output["currency"]);
    }

    [Fact]
    public async Task ComputeBalance_EmitsVaultBalanceComputedEvent()
    {
        var context = CreateContext(ValidBalanceData());

        var result = await _engine.ExecuteAsync(context);

        Assert.True(result.Success);
        Assert.Single(result.Events);
        Assert.Contains(result.Events, e => e.EventType == "VaultBalanceComputed");
    }

    [Fact]
    public async Task ComputeBalance_EmptyLedger_ReturnsZeroBalance()
    {
        var data = ValidBalanceData(new List<IReadOnlyDictionary<string, object>>());
        var context = CreateContext(data);

        var result = await _engine.ExecuteAsync(context);

        Assert.True(result.Success);
        Assert.Equal(0m, result.Output["currentBalance"]);
        Assert.Equal(0m, result.Output["totalCredits"]);
        Assert.Equal(0m, result.Output["totalDebits"]);
    }

    [Fact]
    public async Task ComputeBalance_NoLedgerEntries_ReturnsZeroBalance()
    {
        var data = new Dictionary<string, object>
        {
            ["vaultId"] = TestVaultId,
            ["vaultAccountId"] = TestAccountId,
            ["currency"] = "GBP"
        };
        var context = CreateContext(data);

        var result = await _engine.ExecuteAsync(context);

        Assert.True(result.Success);
        Assert.Equal(0m, result.Output["currentBalance"]);
    }

    [Fact]
    public async Task ComputeBalance_CreditsAndDebits_AggregatedCorrectly()
    {
        var entries = CreateLedgerEntries(
            ("Credit", 100m),
            ("Credit", 200m),
            ("Credit", 300m),
            ("Debit", 50m),
            ("Debit", 75m)
        );
        var context = CreateContext(ValidBalanceData(entries));

        var result = await _engine.ExecuteAsync(context);

        Assert.True(result.Success);
        Assert.Equal(600m, result.Output["totalCredits"]);
        Assert.Equal(125m, result.Output["totalDebits"]);
        Assert.Equal(475m, result.Output["currentBalance"]);
    }

    [Fact]
    public async Task ComputeBalance_FiltersByVaultAccountId()
    {
        var entries = new List<IReadOnlyDictionary<string, object>>
        {
            new Dictionary<string, object>
            {
                ["vaultAccountId"] = TestAccountId,
                ["currency"] = "GBP",
                ["amount"] = 1000m,
                ["direction"] = "Credit"
            },
            new Dictionary<string, object>
            {
                ["vaultAccountId"] = Guid.NewGuid().ToString(),
                ["currency"] = "GBP",
                ["amount"] = 5000m,
                ["direction"] = "Credit"
            }
        };
        var context = CreateContext(ValidBalanceData(entries));

        var result = await _engine.ExecuteAsync(context);

        Assert.True(result.Success);
        Assert.Equal(1000m, result.Output["totalCredits"]);
        Assert.Equal(1000m, result.Output["currentBalance"]);
    }

    [Fact]
    public async Task ComputeBalance_FiltersByCurrency()
    {
        var entries = new List<IReadOnlyDictionary<string, object>>
        {
            new Dictionary<string, object>
            {
                ["vaultAccountId"] = TestAccountId,
                ["currency"] = "GBP",
                ["amount"] = 1000m,
                ["direction"] = "Credit"
            },
            new Dictionary<string, object>
            {
                ["vaultAccountId"] = TestAccountId,
                ["currency"] = "USD",
                ["amount"] = 5000m,
                ["direction"] = "Credit"
            }
        };
        var context = CreateContext(ValidBalanceData(entries));

        var result = await _engine.ExecuteAsync(context);

        Assert.True(result.Success);
        Assert.Equal(1000m, result.Output["totalCredits"]);
        Assert.Equal(1000m, result.Output["currentBalance"]);
    }

    [Fact]
    public async Task ComputeBalance_MissingVaultId_Fails()
    {
        var data = ValidBalanceData();
        data.Remove("vaultId");
        var context = CreateContext(data);

        var result = await _engine.ExecuteAsync(context);

        Assert.False(result.Success);
    }

    [Fact]
    public async Task ComputeBalance_MissingVaultAccountId_Fails()
    {
        var data = ValidBalanceData();
        data.Remove("vaultAccountId");
        var context = CreateContext(data);

        var result = await _engine.ExecuteAsync(context);

        Assert.False(result.Success);
    }

    [Fact]
    public async Task ComputeBalance_MissingCurrency_Fails()
    {
        var data = ValidBalanceData();
        data.Remove("currency");
        var context = CreateContext(data);

        var result = await _engine.ExecuteAsync(context);

        Assert.False(result.Success);
    }

    [Fact]
    public async Task ComputeBalance_UnsupportedCurrency_Fails()
    {
        var data = ValidBalanceData();
        data["currency"] = "BTC";
        var context = CreateContext(data);

        var result = await _engine.ExecuteAsync(context);

        Assert.False(result.Success);
    }

    [Fact]
    public async Task ComputeBalance_InvalidVaultIdFormat_Fails()
    {
        var data = ValidBalanceData();
        data["vaultId"] = "not-a-guid";
        var context = CreateContext(data);

        var result = await _engine.ExecuteAsync(context);

        Assert.False(result.Success);
    }

    [Fact]
    public async Task ComputeBalance_EventPayloadContainsTopic()
    {
        var context = CreateContext(ValidBalanceData());

        var result = await _engine.ExecuteAsync(context);

        Assert.True(result.Success);
        var evt = Assert.Single(result.Events);
        Assert.Equal("whyce.economic.events", evt.Payload["topic"]);
    }

    [Fact]
    public async Task ComputeBalance_DefaultBalanceScope_IsCurrent()
    {
        var context = CreateContext(ValidBalanceData());

        var result = await _engine.ExecuteAsync(context);

        Assert.True(result.Success);
        Assert.Equal("Current", result.Output["balanceScope"]);
    }
}
