namespace Whycespace.Tests.ExecutionEngines;

using Whycespace.Engines.T2E.Economic.Vault;
using Whycespace.Contracts.Engines;
using Xunit;

public sealed class VaultSnapshotEngineTests
{
    private readonly VaultSnapshotEngine _engine = new();

    private static readonly string TestVaultId = Guid.NewGuid().ToString();
    private static readonly string TestRequestedBy = Guid.NewGuid().ToString();
    private static readonly string TestSnapshotId = Guid.NewGuid().ToString();

    private static EngineContext CreateContext(Dictionary<string, object> data) =>
        new(Guid.NewGuid(), Guid.NewGuid().ToString(), "CreateVaultSnapshot", "partition-1", data);

    private static List<IReadOnlyDictionary<string, object>> CreateLedgerEntries(params (string direction, decimal amount)[] entries)
    {
        var list = new List<IReadOnlyDictionary<string, object>>();
        foreach (var (direction, amount) in entries)
        {
            list.Add(new Dictionary<string, object>
            {
                ["amount"] = amount,
                ["direction"] = direction
            });
        }
        return list;
    }

    private static List<object> CreateTransactions(int count)
    {
        var list = new List<object>();
        for (var i = 0; i < count; i++)
            list.Add(new Dictionary<string, object> { ["transactionId"] = Guid.NewGuid().ToString() });
        return list;
    }

    private static List<object> CreateParticipants(int count)
    {
        var list = new List<object>();
        for (var i = 0; i < count; i++)
            list.Add(new Dictionary<string, object> { ["participantId"] = Guid.NewGuid().ToString() });
        return list;
    }

    private static Dictionary<string, object> ValidData(
        List<IReadOnlyDictionary<string, object>>? ledgerEntries = null,
        List<object>? transactions = null,
        List<object>? participants = null) =>
        new()
        {
            ["snapshotId"] = TestSnapshotId,
            ["vaultId"] = TestVaultId,
            ["snapshotTimestamp"] = DateTime.UtcNow.ToString("O"),
            ["requestedBy"] = TestRequestedBy,
            ["ledgerEntries"] = ledgerEntries ?? CreateLedgerEntries(
                ("Credit", 50000m),
                ("Credit", 20000m),
                ("Debit", 10000m)
            ),
            ["transactions"] = transactions ?? CreateTransactions(5),
            ["participants"] = participants ?? CreateParticipants(3)
        };

    // --- Snapshot Creation ---

    [Fact]
    public async Task CreateSnapshot_ValidRequest_Succeeds()
    {
        var context = CreateContext(ValidData());

        var result = await _engine.ExecuteAsync(context);

        Assert.True(result.Success);
        Assert.Equal("Created", result.Output["snapshotStatus"]);
        Assert.Equal(TestSnapshotId, result.Output["snapshotId"]);
        Assert.Equal(TestVaultId, result.Output["vaultId"]);
    }

    [Fact]
    public async Task CreateSnapshot_EmitsRequestedAndCreatedEvents()
    {
        var context = CreateContext(ValidData());

        var result = await _engine.ExecuteAsync(context);

        Assert.True(result.Success);
        Assert.Equal(2, result.Events.Count);
        Assert.Contains(result.Events, e => e.EventType == "VaultSnapshotRequested");
        Assert.Contains(result.Events, e => e.EventType == "VaultSnapshotCreated");
    }

    [Fact]
    public async Task CreateSnapshot_EventPayloadsContainTopic()
    {
        var context = CreateContext(ValidData());

        var result = await _engine.ExecuteAsync(context);

        Assert.True(result.Success);
        foreach (var evt in result.Events)
            Assert.Equal("whyce.economic.events", evt.Payload["topic"]);
    }

    // --- Balance Computation ---

    [Fact]
    public async Task CreateSnapshot_BalanceDerivedFromLedger()
    {
        var entries = CreateLedgerEntries(
            ("Credit", 100000m),
            ("Credit", 50000m),
            ("Debit", 25000m)
        );
        var context = CreateContext(ValidData(ledgerEntries: entries));

        var result = await _engine.ExecuteAsync(context);

        Assert.True(result.Success);
        Assert.Equal(125000m, result.Output["vaultBalance"]);
    }

    [Fact]
    public async Task CreateSnapshot_EmptyLedger_ZeroBalance()
    {
        var entries = new List<IReadOnlyDictionary<string, object>>();
        var context = CreateContext(ValidData(ledgerEntries: entries));

        var result = await _engine.ExecuteAsync(context);

        Assert.True(result.Success);
        Assert.Equal(0m, result.Output["vaultBalance"]);
    }

    [Fact]
    public async Task CreateSnapshot_NoLedgerEntries_ZeroBalance()
    {
        var data = ValidData();
        data.Remove("ledgerEntries");
        var context = CreateContext(data);

        var result = await _engine.ExecuteAsync(context);

        Assert.True(result.Success);
        Assert.Equal(0m, result.Output["vaultBalance"]);
    }

    // --- Transaction Count ---

    [Fact]
    public async Task CreateSnapshot_TransactionCountCaptured()
    {
        var transactions = CreateTransactions(7);
        var context = CreateContext(ValidData(transactions: transactions));

        var result = await _engine.ExecuteAsync(context);

        Assert.True(result.Success);
        Assert.Equal(7, result.Output["transactionCount"]);
    }

    [Fact]
    public async Task CreateSnapshot_NoTransactions_ZeroCount()
    {
        var data = ValidData();
        data.Remove("transactions");
        var context = CreateContext(data);

        var result = await _engine.ExecuteAsync(context);

        Assert.True(result.Success);
        Assert.Equal(0, result.Output["transactionCount"]);
    }

    // --- Participant Count ---

    [Fact]
    public async Task CreateSnapshot_ParticipantCountCaptured()
    {
        var participants = CreateParticipants(4);
        var context = CreateContext(ValidData(participants: participants));

        var result = await _engine.ExecuteAsync(context);

        Assert.True(result.Success);
        Assert.Equal(4, result.Output["participantCount"]);
    }

    [Fact]
    public async Task CreateSnapshot_NoParticipants_ZeroCount()
    {
        var data = ValidData();
        data.Remove("participants");
        var context = CreateContext(data);

        var result = await _engine.ExecuteAsync(context);

        Assert.True(result.Success);
        Assert.Equal(0, result.Output["participantCount"]);
    }

    // --- Deterministic ---

    [Fact]
    public async Task CreateSnapshot_SameInputs_SameBalanceAndCounts()
    {
        var entries = CreateLedgerEntries(("Credit", 1000m), ("Debit", 200m));
        var transactions = CreateTransactions(3);
        var participants = CreateParticipants(2);

        var data1 = ValidData(ledgerEntries: entries, transactions: transactions, participants: participants);
        var data2 = ValidData(ledgerEntries: entries, transactions: transactions, participants: participants);

        var result1 = await _engine.ExecuteAsync(CreateContext(data1));
        var result2 = await _engine.ExecuteAsync(CreateContext(data2));

        Assert.True(result1.Success);
        Assert.True(result2.Success);
        Assert.Equal(result1.Output["vaultBalance"], result2.Output["vaultBalance"]);
        Assert.Equal(result1.Output["transactionCount"], result2.Output["transactionCount"]);
        Assert.Equal(result1.Output["participantCount"], result2.Output["participantCount"]);
    }

    // --- Snapshot Hash ---

    [Fact]
    public async Task CreateSnapshot_IncludesSnapshotHash()
    {
        var context = CreateContext(ValidData());

        var result = await _engine.ExecuteAsync(context);

        Assert.True(result.Success);
        Assert.True(result.Output.ContainsKey("snapshotHash"));
        var hash = result.Output["snapshotHash"] as string;
        Assert.False(string.IsNullOrEmpty(hash));
    }

    // --- Default Scope ---

    [Fact]
    public async Task CreateSnapshot_DefaultScope_IsFullVault()
    {
        var context = CreateContext(ValidData());

        var result = await _engine.ExecuteAsync(context);

        Assert.True(result.Success);
        Assert.Equal("FullVault", result.Output["snapshotScope"]);
    }

    [Fact]
    public async Task CreateSnapshot_CustomScope_IsPreserved()
    {
        var data = ValidData();
        data["snapshotScope"] = "Account";
        var context = CreateContext(data);

        var result = await _engine.ExecuteAsync(context);

        Assert.True(result.Success);
        Assert.Equal("Account", result.Output["snapshotScope"]);
    }

    // --- Validation ---

    [Fact]
    public async Task CreateSnapshot_MissingSnapshotId_Fails()
    {
        var data = ValidData();
        data.Remove("snapshotId");
        var context = CreateContext(data);

        var result = await _engine.ExecuteAsync(context);

        Assert.False(result.Success);
    }

    [Fact]
    public async Task CreateSnapshot_InvalidSnapshotIdFormat_Fails()
    {
        var data = ValidData();
        data["snapshotId"] = "not-a-guid";
        var context = CreateContext(data);

        var result = await _engine.ExecuteAsync(context);

        Assert.False(result.Success);
    }

    [Fact]
    public async Task CreateSnapshot_MissingVaultId_Fails()
    {
        var data = ValidData();
        data.Remove("vaultId");
        var context = CreateContext(data);

        var result = await _engine.ExecuteAsync(context);

        Assert.False(result.Success);
    }

    [Fact]
    public async Task CreateSnapshot_InvalidVaultIdFormat_Fails()
    {
        var data = ValidData();
        data["vaultId"] = "invalid";
        var context = CreateContext(data);

        var result = await _engine.ExecuteAsync(context);

        Assert.False(result.Success);
    }

    [Fact]
    public async Task CreateSnapshot_MissingSnapshotTimestamp_Fails()
    {
        var data = ValidData();
        data.Remove("snapshotTimestamp");
        var context = CreateContext(data);

        var result = await _engine.ExecuteAsync(context);

        Assert.False(result.Success);
    }

    [Fact]
    public async Task CreateSnapshot_InvalidSnapshotTimestamp_Fails()
    {
        var data = ValidData();
        data["snapshotTimestamp"] = "not-a-date";
        var context = CreateContext(data);

        var result = await _engine.ExecuteAsync(context);

        Assert.False(result.Success);
    }

    [Fact]
    public async Task CreateSnapshot_MissingRequestedBy_Fails()
    {
        var data = ValidData();
        data.Remove("requestedBy");
        var context = CreateContext(data);

        var result = await _engine.ExecuteAsync(context);

        Assert.False(result.Success);
    }

    [Fact]
    public async Task CreateSnapshot_InvalidRequestedByFormat_Fails()
    {
        var data = ValidData();
        data["requestedBy"] = "bad-guid";
        var context = CreateContext(data);

        var result = await _engine.ExecuteAsync(context);

        Assert.False(result.Success);
    }
}
