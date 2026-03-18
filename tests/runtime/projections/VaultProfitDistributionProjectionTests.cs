
using System.Text.Json;
using Whycespace.Shared.Envelopes;
using Whycespace.Shared.Primitives.Common;
using Whycespace.Contracts.Events;
using Whycespace.Shared.Primitives.Common;
using Whycespace.Systems.Midstream.WhyceAtlas.Projections;
using Whycespace.Systems.Midstream.WhyceAtlas.Projections.Models;
using Whycespace.ProjectionRuntime.Storage;

namespace Whycespace.ProjectionRuntime.Projections.Tests;

public sealed class VaultProfitDistributionProjectionTests
{
    private readonly RedisProjectionStore _store = new();
    private readonly VaultProfitDistributionProjection _projection;

    public VaultProfitDistributionProjectionTests()
    {
        _projection = new VaultProfitDistributionProjection(_store);
    }

    [Fact]
    public async Task HandleAsync_ProfitDistributed_CreatesDistributionRecord()
    {
        var vaultId = Guid.NewGuid();
        var distributionId = Guid.NewGuid();
        var participantId = Guid.NewGuid();

        var envelope = CreateEnvelope("VaultProfitDistributedEvent", vaultId, new Dictionary<string, object>
        {
            ["distributionId"] = distributionId.ToString(),
            ["vaultId"] = vaultId.ToString(),
            ["participantId"] = participantId.ToString(),
            ["profitAmount"] = 1500m,
            ["currency"] = "WHY",
            ["distributionType"] = "ParticipantProfit",
            ["distributionReference"] = "REF-001"
        });

        await _projection.HandleAsync(envelope);

        var key = $"vault-profit-dist:{vaultId}:{distributionId}:{participantId}";
        var result = await _store.GetAsync(key);
        Assert.NotNull(result);

        var model = JsonSerializer.Deserialize<VaultProfitDistributionReadModel>(result);
        Assert.NotNull(model);
        Assert.Equal(vaultId, model.VaultId);
        Assert.Equal(participantId, model.ParticipantId);
        Assert.Equal(1500m, model.ProfitAmount);
        Assert.Equal("WHY", model.Currency);
        Assert.Equal("ParticipantProfit", model.DistributionType);
        Assert.Equal("REF-001", model.DistributionReference);
    }

    [Fact]
    public async Task HandleAsync_ProfitDistributed_TracksParticipantAllocation()
    {
        var vaultId = Guid.NewGuid();
        var participantId = Guid.NewGuid();
        var dist1 = Guid.NewGuid();
        var dist2 = Guid.NewGuid();

        await _projection.HandleAsync(CreateEnvelope("VaultProfitDistributedEvent", vaultId, new Dictionary<string, object>
        {
            ["distributionId"] = dist1.ToString(),
            ["vaultId"] = vaultId.ToString(),
            ["participantId"] = participantId.ToString(),
            ["profitAmount"] = 1000m,
            ["currency"] = "WHY",
            ["distributionType"] = "ParticipantProfit"
        }));

        await _projection.HandleAsync(CreateEnvelope("VaultProfitDistributedEvent", vaultId, new Dictionary<string, object>
        {
            ["distributionId"] = dist2.ToString(),
            ["vaultId"] = vaultId.ToString(),
            ["participantId"] = participantId.ToString(),
            ["profitAmount"] = 2000m,
            ["currency"] = "WHY",
            ["distributionType"] = "ParticipantProfit"
        }));

        var indexKey = $"vault-profit-dist-participant:{vaultId}:{participantId}";
        var indexResult = await _store.GetAsync(indexKey);
        Assert.NotNull(indexResult);

        var keys = JsonSerializer.Deserialize<List<string>>(indexResult);
        Assert.NotNull(keys);
        Assert.Equal(2, keys.Count);
    }

    [Fact]
    public async Task HandleAsync_DuplicateEvent_IsIdempotent()
    {
        var vaultId = Guid.NewGuid();
        var distributionId = Guid.NewGuid();
        var participantId = Guid.NewGuid();

        var payload = new Dictionary<string, object>
        {
            ["distributionId"] = distributionId.ToString(),
            ["vaultId"] = vaultId.ToString(),
            ["participantId"] = participantId.ToString(),
            ["profitAmount"] = 500m,
            ["currency"] = "WHY",
            ["distributionType"] = "OperatorProfit"
        };

        await _projection.HandleAsync(CreateEnvelope("VaultProfitDistributedEvent", vaultId, payload));
        await _projection.HandleAsync(CreateEnvelope("VaultProfitDistributedEvent", vaultId, payload));

        var indexKey = $"vault-profit-dist-index:{vaultId}";
        var indexResult = await _store.GetAsync(indexKey);
        Assert.NotNull(indexResult);

        var keys = JsonSerializer.Deserialize<List<string>>(indexResult);
        Assert.NotNull(keys);
        Assert.Single(keys);
    }

    [Fact]
    public async Task HandleAsync_DistributionAdjusted_UpdatesAmount()
    {
        var vaultId = Guid.NewGuid();
        var distributionId = Guid.NewGuid();
        var participantId = Guid.NewGuid();

        await _projection.HandleAsync(CreateEnvelope("VaultProfitDistributedEvent", vaultId, new Dictionary<string, object>
        {
            ["distributionId"] = distributionId.ToString(),
            ["vaultId"] = vaultId.ToString(),
            ["participantId"] = participantId.ToString(),
            ["profitAmount"] = 1000m,
            ["currency"] = "WHY",
            ["distributionType"] = "ParticipantProfit"
        }));

        await _projection.HandleAsync(CreateEnvelope("VaultProfitDistributionAdjustedEvent", vaultId, new Dictionary<string, object>
        {
            ["distributionId"] = distributionId.ToString(),
            ["vaultId"] = vaultId.ToString(),
            ["participantId"] = participantId.ToString(),
            ["profitAmount"] = 1200m
        }));

        var key = $"vault-profit-dist:{vaultId}:{distributionId}:{participantId}";
        var result = await _store.GetAsync(key);
        Assert.NotNull(result);

        var model = JsonSerializer.Deserialize<VaultProfitDistributionReadModel>(result);
        Assert.NotNull(model);
        Assert.Equal(1200m, model.ProfitAmount);
    }

    [Fact]
    public async Task HandleAsync_DistributionRecorded_UpdatesSummary()
    {
        var vaultId = Guid.NewGuid();
        var distributionId = Guid.NewGuid();
        var participantId = Guid.NewGuid();

        await _projection.HandleAsync(CreateEnvelope("VaultProfitDistributedEvent", vaultId, new Dictionary<string, object>
        {
            ["distributionId"] = distributionId.ToString(),
            ["vaultId"] = vaultId.ToString(),
            ["participantId"] = participantId.ToString(),
            ["profitAmount"] = 750m,
            ["currency"] = "WHY",
            ["distributionType"] = "GovernanceProfit"
        }));

        await _projection.HandleAsync(CreateEnvelope("VaultProfitDistributionRecordedEvent", vaultId, new Dictionary<string, object>
        {
            ["distributionId"] = distributionId.ToString(),
            ["vaultId"] = vaultId.ToString(),
            ["participantId"] = participantId.ToString(),
            ["distributionSummary"] = "Q1 governance profit allocation"
        }));

        var key = $"vault-profit-dist:{vaultId}:{distributionId}:{participantId}";
        var result = await _store.GetAsync(key);
        Assert.NotNull(result);

        var model = JsonSerializer.Deserialize<VaultProfitDistributionReadModel>(result);
        Assert.NotNull(model);
        Assert.Equal("Q1 governance profit allocation", model.DistributionSummary);
        Assert.Equal(750m, model.ProfitAmount);
    }

    [Fact]
    public async Task ProjectionReplay_RebuildsStateFromEvents()
    {
        var vaultId = Guid.NewGuid();
        var participantA = Guid.NewGuid();
        var participantB = Guid.NewGuid();
        var dist1 = Guid.NewGuid();
        var dist2 = Guid.NewGuid();

        var events = new[]
        {
            CreateEnvelope("VaultProfitDistributedEvent", vaultId, new Dictionary<string, object>
            {
                ["distributionId"] = dist1.ToString(),
                ["vaultId"] = vaultId.ToString(),
                ["participantId"] = participantA.ToString(),
                ["profitAmount"] = 3000m,
                ["currency"] = "WHY",
                ["distributionType"] = "ParticipantProfit"
            }),
            CreateEnvelope("VaultProfitDistributedEvent", vaultId, new Dictionary<string, object>
            {
                ["distributionId"] = dist2.ToString(),
                ["vaultId"] = vaultId.ToString(),
                ["participantId"] = participantB.ToString(),
                ["profitAmount"] = 2000m,
                ["currency"] = "WHY",
                ["distributionType"] = "ReserveAllocation"
            })
        };

        // Simulate replay
        var replayStore = new RedisProjectionStore();
        var replayProjection = new VaultProfitDistributionProjection(replayStore);

        foreach (var envelope in events)
            await replayProjection.HandleAsync(envelope);

        var keyA = $"vault-profit-dist:{vaultId}:{dist1}:{participantA}";
        var keyB = $"vault-profit-dist:{vaultId}:{dist2}:{participantB}";

        var resultA = await replayStore.GetAsync(keyA);
        var resultB = await replayStore.GetAsync(keyB);

        Assert.NotNull(resultA);
        Assert.NotNull(resultB);

        var modelA = JsonSerializer.Deserialize<VaultProfitDistributionReadModel>(resultA);
        var modelB = JsonSerializer.Deserialize<VaultProfitDistributionReadModel>(resultB);

        Assert.Equal(3000m, modelA!.ProfitAmount);
        Assert.Equal("ParticipantProfit", modelA.DistributionType);
        Assert.Equal(2000m, modelB!.ProfitAmount);
        Assert.Equal("ReserveAllocation", modelB.DistributionType);
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
