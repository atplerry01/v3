using System.Text.Json;
using Whycespace.Contracts.Primitives;
using Whycespace.EventFabric.Models;
using Whycespace.Projections.Core.Economics;
using Whycespace.Projections.Core.Economics.Models;
using Whycespace.ProjectionRuntime.Storage;

namespace Whycespace.Projections.Tests;

public sealed class VaultBalanceProjectionTests
{
    private readonly RedisProjectionStore _store = new();
    private readonly VaultBalanceProjection _projection;

    public VaultBalanceProjectionTests()
    {
        _projection = new VaultBalanceProjection(_store);
    }

    [Fact]
    public async Task HandleAsync_VaultCreated_InitializesBalance()
    {
        var vaultId = Guid.NewGuid().ToString();
        var envelope = CreateEnvelope("VaultCreated", vaultId, new Dictionary<string, object>
        {
            ["vaultId"] = vaultId,
            ["balance"] = 0m,
            ["status"] = "Active"
        });

        await _projection.HandleAsync(envelope);

        var model = await LoadModelAsync(vaultId);
        Assert.NotNull(model);
        Assert.Equal(0m, model.CurrentBalance);
        Assert.Equal("Active", model.VaultStatus);
    }

    [Fact]
    public async Task HandleAsync_ContributionCompleted_IncreasesBalance()
    {
        var vaultId = Guid.NewGuid().ToString();
        await SeedVault(vaultId);

        var envelope = CreateEnvelope("VaultContributionCompleted", vaultId, new Dictionary<string, object>
        {
            ["vaultId"] = vaultId,
            ["amount"] = 5000m
        });

        await _projection.HandleAsync(envelope);

        var model = await LoadModelAsync(vaultId);
        Assert.NotNull(model);
        Assert.Equal(5000m, model.CurrentBalance);
        Assert.Equal(5000m, model.TotalCredits);
        Assert.Equal(0m, model.TotalDebits);
        Assert.Equal(1, model.TransactionCount);
    }

    [Fact]
    public async Task HandleAsync_WithdrawalCompleted_DecreasesBalance()
    {
        var vaultId = Guid.NewGuid().ToString();
        await SeedVaultWithBalance(vaultId, 10000m);

        var envelope = CreateEnvelope("VaultWithdrawalCompleted", vaultId, new Dictionary<string, object>
        {
            ["vaultId"] = vaultId,
            ["amount"] = 3000m
        });

        await _projection.HandleAsync(envelope);

        var model = await LoadModelAsync(vaultId);
        Assert.NotNull(model);
        Assert.Equal(7000m, model.CurrentBalance);
        Assert.Equal(3000m, model.TotalDebits);
    }

    [Fact]
    public async Task HandleAsync_TransferCompleted_DebitsSourceVault()
    {
        var sourceVaultId = Guid.NewGuid().ToString();
        await SeedVaultWithBalance(sourceVaultId, 10000m);

        var envelope = CreateEnvelope("VaultTransferCompleted", sourceVaultId, new Dictionary<string, object>
        {
            ["vaultId"] = sourceVaultId,
            ["sourceVaultId"] = sourceVaultId,
            ["destinationVaultId"] = Guid.NewGuid().ToString(),
            ["amount"] = 2000m
        });

        await _projection.HandleAsync(envelope);

        var model = await LoadModelAsync(sourceVaultId);
        Assert.NotNull(model);
        Assert.Equal(8000m, model.CurrentBalance);
        Assert.Equal(2000m, model.TotalDebits);
    }

    [Fact]
    public async Task HandleAsync_ProfitDistributionCompleted_DebitsBalance()
    {
        var vaultId = Guid.NewGuid().ToString();
        await SeedVaultWithBalance(vaultId, 20000m);

        var envelope = CreateEnvelope("VaultProfitDistributionCompleted", vaultId, new Dictionary<string, object>
        {
            ["vaultId"] = vaultId,
            ["totalDistributed"] = 5000m
        });

        await _projection.HandleAsync(envelope);

        var model = await LoadModelAsync(vaultId);
        Assert.NotNull(model);
        Assert.Equal(15000m, model.CurrentBalance);
        Assert.Equal(5000m, model.TotalDebits);
    }

    [Fact]
    public async Task HandleAsync_TransactionCompleted_CreditDirection_IncreasesBalance()
    {
        var vaultId = Guid.NewGuid().ToString();
        await SeedVault(vaultId);

        var envelope = CreateEnvelope("VaultTransactionCompleted", vaultId, new Dictionary<string, object>
        {
            ["vaultId"] = vaultId,
            ["amount"] = 1500m,
            ["ledgerDirection"] = "Credit"
        });

        await _projection.HandleAsync(envelope);

        var model = await LoadModelAsync(vaultId);
        Assert.NotNull(model);
        Assert.Equal(1500m, model.CurrentBalance);
        Assert.Equal(1500m, model.TotalCredits);
    }

    [Fact]
    public async Task HandleAsync_TransactionCompleted_DebitDirection_DecreasesBalance()
    {
        var vaultId = Guid.NewGuid().ToString();
        await SeedVaultWithBalance(vaultId, 5000m);

        var envelope = CreateEnvelope("VaultTransactionCompleted", vaultId, new Dictionary<string, object>
        {
            ["vaultId"] = vaultId,
            ["amount"] = 1000m,
            ["ledgerDirection"] = "Debit"
        });

        await _projection.HandleAsync(envelope);

        var model = await LoadModelAsync(vaultId);
        Assert.NotNull(model);
        Assert.Equal(4000m, model.CurrentBalance);
    }

    [Fact]
    public async Task HandleAsync_IdempotentDuplicateEvents_DoNotCorruptState()
    {
        var vaultId = Guid.NewGuid().ToString();
        await SeedVault(vaultId);

        var envelope = CreateEnvelope("VaultContributionCompleted", vaultId, new Dictionary<string, object>
        {
            ["vaultId"] = vaultId,
            ["amount"] = 1000m
        });

        await _projection.HandleAsync(envelope);
        await _projection.HandleAsync(envelope);

        var model = await LoadModelAsync(vaultId);
        Assert.NotNull(model);
        Assert.Equal(2000m, model.CurrentBalance);
        Assert.Equal(2, model.TransactionCount);
    }

    [Fact]
    public async Task HandleAsync_MultipleEvents_AccumulateCorrectly()
    {
        var vaultId = Guid.NewGuid().ToString();
        await SeedVault(vaultId);

        await _projection.HandleAsync(CreateEnvelope("VaultContributionCompleted", vaultId, new Dictionary<string, object>
        {
            ["vaultId"] = vaultId,
            ["amount"] = 10000m
        }));

        await _projection.HandleAsync(CreateEnvelope("VaultWithdrawalCompleted", vaultId, new Dictionary<string, object>
        {
            ["vaultId"] = vaultId,
            ["amount"] = 3000m
        }));

        await _projection.HandleAsync(CreateEnvelope("VaultContributionCompleted", vaultId, new Dictionary<string, object>
        {
            ["vaultId"] = vaultId,
            ["amount"] = 2000m
        }));

        var model = await LoadModelAsync(vaultId);
        Assert.NotNull(model);
        Assert.Equal(9000m, model.CurrentBalance);
        Assert.Equal(12000m, model.TotalCredits);
        Assert.Equal(3000m, model.TotalDebits);
        Assert.Equal(3, model.TransactionCount);
    }

    [Fact]
    public async Task HandleAsync_ReplayRebuildsProjectionCorrectly()
    {
        var vaultId = Guid.NewGuid().ToString();

        var events = new[]
        {
            CreateEnvelope("VaultCreated", vaultId, new Dictionary<string, object>
            {
                ["vaultId"] = vaultId, ["balance"] = 0m, ["status"] = "Active"
            }),
            CreateEnvelope("VaultContributionCompleted", vaultId, new Dictionary<string, object>
            {
                ["vaultId"] = vaultId, ["amount"] = 5000m
            }),
            CreateEnvelope("VaultWithdrawalCompleted", vaultId, new Dictionary<string, object>
            {
                ["vaultId"] = vaultId, ["amount"] = 1000m
            }),
            CreateEnvelope("VaultProfitDistributionCompleted", vaultId, new Dictionary<string, object>
            {
                ["vaultId"] = vaultId, ["totalDistributed"] = 500m
            })
        };

        foreach (var evt in events)
            await _projection.HandleAsync(evt);

        var model = await LoadModelAsync(vaultId);
        Assert.NotNull(model);
        Assert.Equal(3500m, model.CurrentBalance);
        Assert.Equal(5000m, model.TotalCredits);
        Assert.Equal(1500m, model.TotalDebits);
        Assert.Equal(3, model.TransactionCount);
        Assert.Equal("Active", model.VaultStatus);
    }

    [Fact]
    public async Task HandleAsync_UnknownEventType_DoesNotCorruptState()
    {
        var vaultId = Guid.NewGuid().ToString();
        await SeedVaultWithBalance(vaultId, 1000m);

        var envelope = CreateEnvelope("SomeUnknownEvent", vaultId, new Dictionary<string, object>
        {
            ["vaultId"] = vaultId,
            ["amount"] = 999m
        });

        await _projection.HandleAsync(envelope);

        var model = await LoadModelAsync(vaultId);
        Assert.NotNull(model);
        Assert.Equal(1000m, model.CurrentBalance);
    }

    [Fact]
    public async Task HandleAsync_MissingVaultId_DoesNotThrow()
    {
        var envelope = CreateEnvelope("VaultContributionCompleted", null, new Dictionary<string, object>
        {
            ["amount"] = 1000m
        });

        await _projection.HandleAsync(envelope);
    }

    [Fact]
    public async Task HandleAsync_LargeEventStream_ProcessesCorrectly()
    {
        var vaultId = Guid.NewGuid().ToString();
        await SeedVault(vaultId);

        const int eventCount = 1000;
        for (var i = 0; i < eventCount; i++)
        {
            await _projection.HandleAsync(CreateEnvelope("VaultContributionCompleted", vaultId, new Dictionary<string, object>
            {
                ["vaultId"] = vaultId,
                ["amount"] = 10m
            }));
        }

        var model = await LoadModelAsync(vaultId);
        Assert.NotNull(model);
        Assert.Equal(10000m, model.CurrentBalance);
        Assert.Equal(eventCount, model.TransactionCount);
    }

    private async Task<VaultBalanceModel?> LoadModelAsync(string vaultId)
    {
        var json = await _store.GetAsync($"vault-balance:{vaultId}");
        return json is not null
            ? JsonSerializer.Deserialize<VaultBalanceModel>(json)
            : null;
    }

    private async Task SeedVault(string vaultId)
    {
        var created = CreateEnvelope("VaultCreated", vaultId, new Dictionary<string, object>
        {
            ["vaultId"] = vaultId,
            ["balance"] = 0m,
            ["status"] = "Active"
        });
        await _projection.HandleAsync(created);
    }

    private async Task SeedVaultWithBalance(string vaultId, decimal balance)
    {
        var model = VaultBalanceModel.Initial(vaultId, "Active") with
        {
            CurrentBalance = balance,
            TotalCredits = balance
        };
        await _store.SetAsync($"vault-balance:{vaultId}", JsonSerializer.Serialize(model));
    }

    private static EventEnvelope CreateEnvelope(
        string eventType, string? vaultId, Dictionary<string, object> payload) =>
        new(
            Guid.NewGuid(),
            eventType,
            "whyce.economic.events",
            payload,
            new PartitionKey(vaultId ?? "unknown"),
            Timestamp.Now(),
            AggregateId: vaultId);
}
