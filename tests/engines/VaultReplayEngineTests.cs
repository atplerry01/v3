namespace Whycespace.Tests.Engines;

using Whycespace.Engines.T2E.Economic.Vault;
using Whycespace.Contracts.Engines;
using Xunit;

public sealed class VaultReplayEngineTests
{
    private readonly VaultReplayEngine _engine = new();

    private static readonly string TestVaultId = Guid.NewGuid().ToString();
    private static readonly string TestSnapshotId = Guid.NewGuid().ToString();
    private static readonly string TestRequestedBy = Guid.NewGuid().ToString();
    private static readonly string TestReplayStart = "2026-01-01T00:00:00Z";
    private static readonly string TestReplayEnd = "2026-03-15T00:00:00Z";

    private static EngineContext CreateContext(Dictionary<string, object> data) =>
        new(Guid.NewGuid(), Guid.NewGuid().ToString(), "ExecuteVaultReplay", "partition-1", data);

    private static List<IReadOnlyDictionary<string, object>> CreateLedgerEntries(
        params (string direction, decimal amount, string timestamp)[] entries)
    {
        var list = new List<IReadOnlyDictionary<string, object>>();
        foreach (var (direction, amount, timestamp) in entries)
        {
            list.Add(new Dictionary<string, object>
            {
                ["direction"] = direction,
                ["amount"] = amount,
                ["timestamp"] = timestamp,
                ["transactionId"] = Guid.NewGuid().ToString()
            });
        }
        return list;
    }

    private static Dictionary<string, object> ValidReplayData(
        List<IReadOnlyDictionary<string, object>>? ledgerEntries = null,
        decimal snapshotBalance = 10000m) =>
        new()
        {
            ["replayId"] = Guid.NewGuid().ToString(),
            ["vaultId"] = TestVaultId,
            ["snapshotId"] = TestSnapshotId,
            ["requestedBy"] = TestRequestedBy,
            ["replayStartTimestamp"] = TestReplayStart,
            ["replayEndTimestamp"] = TestReplayEnd,
            ["snapshotBalance"] = snapshotBalance,
            ["ledgerEntries"] = ledgerEntries ?? CreateLedgerEntries(
                ("Credit", 5000m, "2026-01-15T10:00:00Z"),
                ("Credit", 3000m, "2026-02-01T10:00:00Z"),
                ("Debit", 2000m, "2026-02-15T10:00:00Z")
            )
        };

    [Fact]
    public async Task Replay_ValidRequest_Succeeds()
    {
        var context = CreateContext(ValidReplayData());

        var result = await _engine.ExecuteAsync(context);

        Assert.True(result.Success);
        Assert.Equal("Completed", result.Output["replayStatus"]);
    }

    [Fact]
    public async Task Replay_LedgerEntriesReplayedSuccessfully()
    {
        var entries = CreateLedgerEntries(
            ("Credit", 1000m, "2026-01-10T10:00:00Z"),
            ("Credit", 2000m, "2026-02-10T10:00:00Z"),
            ("Debit", 500m, "2026-03-01T10:00:00Z")
        );
        var context = CreateContext(ValidReplayData(entries));

        var result = await _engine.ExecuteAsync(context);

        Assert.True(result.Success);
        Assert.Equal(3, result.Output["replayedLedgerEntryCount"]);
    }

    [Fact]
    public async Task Replay_ChronologicalOrder_EntriesAppliedCorrectly()
    {
        // Entries provided out of order — engine must sort by timestamp
        var entries = CreateLedgerEntries(
            ("Debit", 1000m, "2026-03-01T10:00:00Z"),
            ("Credit", 5000m, "2026-01-10T10:00:00Z"),
            ("Credit", 3000m, "2026-02-10T10:00:00Z")
        );
        var context = CreateContext(ValidReplayData(entries, snapshotBalance: 0m));

        var result = await _engine.ExecuteAsync(context);

        Assert.True(result.Success);
        // 0 + 5000 + 3000 - 1000 = 7000
        Assert.Equal(7000m, result.Output["finalVaultBalance"]);
    }

    [Fact]
    public async Task Replay_BalanceReconstructedCorrectly()
    {
        var entries = CreateLedgerEntries(
            ("Credit", 50000m, "2026-01-05T10:00:00Z"),
            ("Credit", 20000m, "2026-02-05T10:00:00Z"),
            ("Debit", 10000m, "2026-03-05T10:00:00Z")
        );
        var context = CreateContext(ValidReplayData(entries, snapshotBalance: 100000m));

        var result = await _engine.ExecuteAsync(context);

        Assert.True(result.Success);
        // 100000 + 50000 + 20000 - 10000 = 160000
        Assert.Equal(160000m, result.Output["finalVaultBalance"]);
    }

    [Fact]
    public async Task Replay_StatisticsIncludesCorrectCounts()
    {
        var txnId1 = Guid.NewGuid().ToString();
        var txnId2 = Guid.NewGuid().ToString();
        var entries = new List<IReadOnlyDictionary<string, object>>
        {
            new Dictionary<string, object>
            {
                ["direction"] = "Credit",
                ["amount"] = 1000m,
                ["timestamp"] = "2026-01-15T10:00:00Z",
                ["transactionId"] = txnId1
            },
            new Dictionary<string, object>
            {
                ["direction"] = "Credit",
                ["amount"] = 2000m,
                ["timestamp"] = "2026-02-15T10:00:00Z",
                ["transactionId"] = txnId1
            },
            new Dictionary<string, object>
            {
                ["direction"] = "Debit",
                ["amount"] = 500m,
                ["timestamp"] = "2026-03-01T10:00:00Z",
                ["transactionId"] = txnId2
            }
        };
        var context = CreateContext(ValidReplayData(entries));

        var result = await _engine.ExecuteAsync(context);

        Assert.True(result.Success);
        Assert.Equal(3, result.Output["replayedLedgerEntryCount"]);
        Assert.Equal(2, result.Output["replayedTransactionCount"]);
    }

    [Fact]
    public async Task Replay_DeterministicResults()
    {
        var data = ValidReplayData();
        var context1 = CreateContext(new Dictionary<string, object>(data));
        var context2 = CreateContext(new Dictionary<string, object>(data));

        var result1 = await _engine.ExecuteAsync(context1);
        var result2 = await _engine.ExecuteAsync(context2);

        Assert.True(result1.Success);
        Assert.True(result2.Success);
        Assert.Equal(result1.Output["finalVaultBalance"], result2.Output["finalVaultBalance"]);
        Assert.Equal(result1.Output["replayedLedgerEntryCount"], result2.Output["replayedLedgerEntryCount"]);
        Assert.Equal(result1.Output["replayedTransactionCount"], result2.Output["replayedTransactionCount"]);
    }

    [Fact]
    public async Task Replay_EmitsStartedAndCompletedEvents()
    {
        var context = CreateContext(ValidReplayData());

        var result = await _engine.ExecuteAsync(context);

        Assert.True(result.Success);
        Assert.Equal(2, result.Events.Count);
        Assert.Contains(result.Events, e => e.EventType == "VaultReplayStarted");
        Assert.Contains(result.Events, e => e.EventType == "VaultReplayCompleted");
    }

    [Fact]
    public async Task Replay_EventPayloadsContainTopic()
    {
        var context = CreateContext(ValidReplayData());

        var result = await _engine.ExecuteAsync(context);

        Assert.True(result.Success);
        foreach (var evt in result.Events)
        {
            Assert.Equal("whyce.economic.events", evt.Payload["topic"]);
        }
    }

    [Fact]
    public async Task Replay_EmptyLedger_ReturnsSnapshotBalance()
    {
        var context = CreateContext(ValidReplayData(
            new List<IReadOnlyDictionary<string, object>>(),
            snapshotBalance: 5000m));

        var result = await _engine.ExecuteAsync(context);

        Assert.True(result.Success);
        Assert.Equal(5000m, result.Output["finalVaultBalance"]);
        Assert.Equal(0, result.Output["replayedLedgerEntryCount"]);
    }

    [Fact]
    public async Task Replay_FiltersEntriesToReplayWindow()
    {
        var entries = CreateLedgerEntries(
            ("Credit", 1000m, "2025-06-01T10:00:00Z"),  // Before window
            ("Credit", 2000m, "2026-02-01T10:00:00Z"),  // In window
            ("Debit", 500m, "2027-01-01T10:00:00Z")     // After window
        );
        var context = CreateContext(ValidReplayData(entries, snapshotBalance: 0m));

        var result = await _engine.ExecuteAsync(context);

        Assert.True(result.Success);
        Assert.Equal(2000m, result.Output["finalVaultBalance"]);
        Assert.Equal(1, result.Output["replayedLedgerEntryCount"]);
    }

    [Fact]
    public async Task Replay_MissingReplayId_Fails()
    {
        var data = ValidReplayData();
        data.Remove("replayId");
        var context = CreateContext(data);

        var result = await _engine.ExecuteAsync(context);

        Assert.False(result.Success);
    }

    [Fact]
    public async Task Replay_MissingVaultId_Fails()
    {
        var data = ValidReplayData();
        data.Remove("vaultId");
        var context = CreateContext(data);

        var result = await _engine.ExecuteAsync(context);

        Assert.False(result.Success);
    }

    [Fact]
    public async Task Replay_MissingSnapshotId_Fails()
    {
        var data = ValidReplayData();
        data.Remove("snapshotId");
        var context = CreateContext(data);

        var result = await _engine.ExecuteAsync(context);

        Assert.False(result.Success);
    }

    [Fact]
    public async Task Replay_InvalidVaultIdFormat_Fails()
    {
        var data = ValidReplayData();
        data["vaultId"] = "not-a-guid";
        var context = CreateContext(data);

        var result = await _engine.ExecuteAsync(context);

        Assert.False(result.Success);
    }

    [Fact]
    public async Task Replay_EndBeforeStart_Fails()
    {
        var data = ValidReplayData();
        data["replayStartTimestamp"] = "2026-03-15T00:00:00Z";
        data["replayEndTimestamp"] = "2026-01-01T00:00:00Z";
        var context = CreateContext(data);

        var result = await _engine.ExecuteAsync(context);

        Assert.False(result.Success);
    }
}
