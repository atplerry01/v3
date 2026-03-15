using Whycespace.Contracts.Primitives;
using Whycespace.EventFabric.Models;
using Whycespace.Projections.Core.Economics.Vault;
using Whycespace.ProjectionRuntime.Storage;
using System.Text.Json;

namespace Whycespace.Projections.Tests.Vault;

public sealed class VaultTransactionProjectionTests
{
    private readonly RedisProjectionStore _store = new();
    private readonly VaultTransactionProjection _projection;

    public VaultTransactionProjectionTests()
    {
        _projection = new VaultTransactionProjection(_store);
    }

    [Fact]
    public async Task HandleAsync_ContributionRecorded_CreatesTransactionRecord()
    {
        var vaultId = Guid.NewGuid();
        var transactionId = Guid.NewGuid();
        var participantId = Guid.NewGuid();

        var envelope = CreateEnvelope("VaultContributionRecorded", vaultId, new Dictionary<string, object>
        {
            ["transactionId"] = transactionId.ToString(),
            ["vaultId"] = vaultId.ToString(),
            ["participantId"] = participantId.ToString(),
            ["amount"] = 5000m,
            ["currency"] = "WCE"
        });

        await _projection.HandleAsync(envelope);

        var result = await _store.GetAsync($"vault-tx:{vaultId}:{transactionId}");
        Assert.NotNull(result);

        var record = JsonSerializer.Deserialize<VaultTransactionReadModel>(result);
        Assert.NotNull(record);
        Assert.Equal("Contribution", record.TransactionType);
        Assert.Equal(5000m, record.Amount);
        Assert.Equal("Completed", record.TransactionStatus);
        Assert.Equal(vaultId, record.VaultId);
        Assert.Equal(participantId, record.ParticipantId);
    }

    [Fact]
    public async Task HandleAsync_WithdrawalExecuted_CreatesWithdrawalRecord()
    {
        var vaultId = Guid.NewGuid();
        var transactionId = Guid.NewGuid();
        var participantId = Guid.NewGuid();

        var envelope = CreateEnvelope("VaultWithdrawalExecuted", vaultId, new Dictionary<string, object>
        {
            ["transactionId"] = transactionId.ToString(),
            ["vaultId"] = vaultId.ToString(),
            ["participantId"] = participantId.ToString(),
            ["amount"] = 2000m,
            ["currency"] = "WCE"
        });

        await _projection.HandleAsync(envelope);

        var result = await _store.GetAsync($"vault-tx:{vaultId}:{transactionId}");
        Assert.NotNull(result);

        var record = JsonSerializer.Deserialize<VaultTransactionReadModel>(result);
        Assert.NotNull(record);
        Assert.Equal("Withdrawal", record.TransactionType);
        Assert.Equal(2000m, record.Amount);
    }

    [Fact]
    public async Task HandleAsync_TransferExecuted_CreatesTransferRecord()
    {
        var vaultId = Guid.NewGuid();
        var transactionId = Guid.NewGuid();
        var participantId = Guid.NewGuid();

        var envelope = CreateEnvelope("VaultTransferExecuted", vaultId, new Dictionary<string, object>
        {
            ["transactionId"] = transactionId.ToString(),
            ["vaultId"] = vaultId.ToString(),
            ["participantId"] = participantId.ToString(),
            ["amount"] = 1500m,
            ["currency"] = "WCE"
        });

        await _projection.HandleAsync(envelope);

        var result = await _store.GetAsync($"vault-tx:{vaultId}:{transactionId}");
        Assert.NotNull(result);

        var record = JsonSerializer.Deserialize<VaultTransactionReadModel>(result);
        Assert.NotNull(record);
        Assert.Equal("Transfer", record.TransactionType);
        Assert.Equal(1500m, record.Amount);
    }

    [Fact]
    public async Task HandleAsync_ProfitDistributed_CreatesDistributionRecord()
    {
        var vaultId = Guid.NewGuid();
        var transactionId = Guid.NewGuid();
        var participantId = Guid.NewGuid();

        var envelope = CreateEnvelope("VaultProfitDistributed", vaultId, new Dictionary<string, object>
        {
            ["transactionId"] = transactionId.ToString(),
            ["vaultId"] = vaultId.ToString(),
            ["participantId"] = participantId.ToString(),
            ["amount"] = 800m,
            ["currency"] = "WCE"
        });

        await _projection.HandleAsync(envelope);

        var result = await _store.GetAsync($"vault-tx:{vaultId}:{transactionId}");
        Assert.NotNull(result);

        var record = JsonSerializer.Deserialize<VaultTransactionReadModel>(result);
        Assert.NotNull(record);
        Assert.Equal("ProfitDistribution", record.TransactionType);
        Assert.Equal(800m, record.Amount);
    }

    [Fact]
    public async Task HandleAsync_TransactionsStoredChronologically()
    {
        var vaultId = Guid.NewGuid();
        var participantId = Guid.NewGuid();

        var earlier = DateTime.UtcNow.AddHours(-2);
        var later = DateTime.UtcNow.AddHours(-1);

        // Insert later event first
        var tx1 = Guid.NewGuid();
        await _projection.HandleAsync(CreateEnvelope("VaultContributionRecorded", vaultId, new Dictionary<string, object>
        {
            ["transactionId"] = tx1.ToString(),
            ["vaultId"] = vaultId.ToString(),
            ["participantId"] = participantId.ToString(),
            ["amount"] = 100m,
            ["currency"] = "WCE",
            ["timestamp"] = later.ToString("O")
        }));

        // Insert earlier event second
        var tx2 = Guid.NewGuid();
        await _projection.HandleAsync(CreateEnvelope("VaultWithdrawalExecuted", vaultId, new Dictionary<string, object>
        {
            ["transactionId"] = tx2.ToString(),
            ["vaultId"] = vaultId.ToString(),
            ["participantId"] = participantId.ToString(),
            ["amount"] = 50m,
            ["currency"] = "WCE",
            ["timestamp"] = earlier.ToString("O")
        }));

        var indexJson = await _store.GetAsync($"vault-tx-index:{vaultId}");
        Assert.NotNull(indexJson);

        using var doc = JsonDocument.Parse(indexJson);
        var entries = doc.RootElement.EnumerateArray().ToList();
        Assert.Equal(2, entries.Count);

        // Earlier transaction should come first regardless of insertion order
        var firstId = entries[0].GetProperty("TransactionId").GetGuid();
        Assert.Equal(tx2, firstId);
    }

    [Fact]
    public async Task HandleAsync_DuplicateEvent_IsIdempotent()
    {
        var vaultId = Guid.NewGuid();
        var transactionId = Guid.NewGuid();
        var participantId = Guid.NewGuid();

        var envelope = CreateEnvelope("VaultContributionRecorded", vaultId, new Dictionary<string, object>
        {
            ["transactionId"] = transactionId.ToString(),
            ["vaultId"] = vaultId.ToString(),
            ["participantId"] = participantId.ToString(),
            ["amount"] = 5000m,
            ["currency"] = "WCE"
        });

        await _projection.HandleAsync(envelope);
        await _projection.HandleAsync(envelope);

        // Index should only have one entry
        var indexJson = await _store.GetAsync($"vault-tx-index:{vaultId}");
        Assert.NotNull(indexJson);

        using var doc = JsonDocument.Parse(indexJson);
        var entries = doc.RootElement.EnumerateArray().ToList();
        Assert.Single(entries);
    }

    [Fact]
    public async Task HandleAsync_ProjectionReplay_ProducesConsistentState()
    {
        var vaultId = Guid.NewGuid();
        var participantId = Guid.NewGuid();
        var tx1 = Guid.NewGuid();
        var tx2 = Guid.NewGuid();

        var events = new[]
        {
            CreateEnvelope("VaultContributionRecorded", vaultId, new Dictionary<string, object>
            {
                ["transactionId"] = tx1.ToString(),
                ["vaultId"] = vaultId.ToString(),
                ["participantId"] = participantId.ToString(),
                ["amount"] = 1000m,
                ["currency"] = "WCE",
                ["timestamp"] = DateTime.UtcNow.AddHours(-2).ToString("O")
            }),
            CreateEnvelope("VaultWithdrawalExecuted", vaultId, new Dictionary<string, object>
            {
                ["transactionId"] = tx2.ToString(),
                ["vaultId"] = vaultId.ToString(),
                ["participantId"] = participantId.ToString(),
                ["amount"] = 300m,
                ["currency"] = "WCE",
                ["timestamp"] = DateTime.UtcNow.AddHours(-1).ToString("O")
            })
        };

        // First pass
        foreach (var e in events)
            await _projection.HandleAsync(e);

        var firstPassIndex = await _store.GetAsync($"vault-tx-index:{vaultId}");

        // Clear and rebuild (simulate replay)
        var freshStore = new RedisProjectionStore();
        var freshProjection = new VaultTransactionProjection(freshStore);

        foreach (var e in events)
            await freshProjection.HandleAsync(e);

        var replayIndex = await freshStore.GetAsync($"vault-tx-index:{vaultId}");

        Assert.Equal(firstPassIndex, replayIndex);

        var record1 = await freshStore.GetAsync($"vault-tx:{vaultId}:{tx1}");
        var record2 = await freshStore.GetAsync($"vault-tx:{vaultId}:{tx2}");
        Assert.NotNull(record1);
        Assert.NotNull(record2);
    }

    [Fact]
    public async Task HandleAsync_LargeTransactionHistory_HandledEfficiently()
    {
        var vaultId = Guid.NewGuid();
        var participantId = Guid.NewGuid();
        var baseTime = DateTime.UtcNow.AddDays(-30);

        for (var i = 0; i < 100; i++)
        {
            var envelope = CreateEnvelope("VaultContributionRecorded", vaultId, new Dictionary<string, object>
            {
                ["transactionId"] = Guid.NewGuid().ToString(),
                ["vaultId"] = vaultId.ToString(),
                ["participantId"] = participantId.ToString(),
                ["amount"] = (100 + i),
                ["currency"] = "WCE",
                ["timestamp"] = baseTime.AddHours(i).ToString("O")
            });

            await _projection.HandleAsync(envelope);
        }

        var indexJson = await _store.GetAsync($"vault-tx-index:{vaultId}");
        Assert.NotNull(indexJson);

        using var doc = JsonDocument.Parse(indexJson);
        var entries = doc.RootElement.EnumerateArray().ToList();
        Assert.Equal(100, entries.Count);
    }

    private static EventEnvelope CreateEnvelope(string eventType, Guid vaultId, Dictionary<string, object> payload) =>
        new(
            Guid.NewGuid(),
            eventType,
            "whyce.economic.events",
            payload,
            new PartitionKey(vaultId.ToString()),
            Timestamp.Now());
}
