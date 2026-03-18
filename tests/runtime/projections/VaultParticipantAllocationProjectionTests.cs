using System.Text.Json;
using Whycespace.Contracts.Primitives;
using Whycespace.EventFabric.Models;
using Whycespace.Systems.Midstream.WhyceAtlas.Projections.Vault;
using Whycespace.ProjectionRuntime.Storage;

namespace Whycespace.ProjectionRuntime.Projections.Tests;

public sealed class VaultParticipantAllocationProjectionTests
{
    private readonly RedisProjectionStore _store = new();
    private readonly VaultParticipantAllocationProjection _projection;

    public VaultParticipantAllocationProjectionTests()
    {
        _projection = new VaultParticipantAllocationProjection(_store);
    }

    [Fact]
    public async Task HandleAsync_ParticipantAdded_CreatesAllocationRecord()
    {
        var vaultId = Guid.NewGuid();
        var participantId = Guid.NewGuid();

        await HandleEvent("VaultParticipantAdded", vaultId, participantId, new Dictionary<string, object>
        {
            ["vaultId"] = vaultId.ToString(),
            ["participantId"] = participantId.ToString(),
            ["participantRole"] = "Contributor"
        });

        var model = await LoadModel(vaultId, participantId);
        Assert.NotNull(model);
        Assert.Equal(vaultId, model.VaultId);
        Assert.Equal(participantId, model.ParticipantId);
        Assert.Equal("Contributor", model.ParticipantRole);
    }

    [Fact]
    public async Task HandleAsync_AllocationCreated_SetsAllocationPercentage()
    {
        var vaultId = Guid.NewGuid();
        var participantId = Guid.NewGuid();

        await HandleEvent("VaultParticipantAdded", vaultId, participantId, new Dictionary<string, object>
        {
            ["vaultId"] = vaultId.ToString(),
            ["participantId"] = participantId.ToString()
        });

        await HandleEvent("VaultAllocationCreated", vaultId, participantId, new Dictionary<string, object>
        {
            ["vaultId"] = vaultId.ToString(),
            ["participantId"] = participantId.ToString(),
            ["allocationPercentage"] = 25.5m,
            ["profitSharePercentage"] = 20.0m
        });

        var model = await LoadModel(vaultId, participantId);
        Assert.NotNull(model);
        Assert.Equal(25.5m, model.AllocationPercentage);
        Assert.Equal(20.0m, model.ProfitSharePercentage);
    }

    [Fact]
    public async Task HandleAsync_ContributionRecorded_UpdatesContributionAmount()
    {
        var vaultId = Guid.NewGuid();
        var participantId = Guid.NewGuid();

        await HandleEvent("VaultParticipantAdded", vaultId, participantId, new Dictionary<string, object>
        {
            ["vaultId"] = vaultId.ToString(),
            ["participantId"] = participantId.ToString()
        });

        await HandleEvent("VaultContributionRecorded", vaultId, participantId, new Dictionary<string, object>
        {
            ["vaultId"] = vaultId.ToString(),
            ["participantId"] = participantId.ToString(),
            ["amount"] = 5000m
        });

        await HandleEvent("VaultContributionRecorded", vaultId, participantId, new Dictionary<string, object>
        {
            ["vaultId"] = vaultId.ToString(),
            ["participantId"] = participantId.ToString(),
            ["amount"] = 3000m
        });

        var model = await LoadModel(vaultId, participantId);
        Assert.NotNull(model);
        Assert.Equal(8000m, model.ContributionAmount);
    }

    [Fact]
    public async Task HandleAsync_AllocationUpdated_UpdatesPercentages()
    {
        var vaultId = Guid.NewGuid();
        var participantId = Guid.NewGuid();

        await HandleEvent("VaultParticipantAdded", vaultId, participantId, new Dictionary<string, object>
        {
            ["vaultId"] = vaultId.ToString(),
            ["participantId"] = participantId.ToString()
        });

        await HandleEvent("VaultAllocationCreated", vaultId, participantId, new Dictionary<string, object>
        {
            ["vaultId"] = vaultId.ToString(),
            ["participantId"] = participantId.ToString(),
            ["allocationPercentage"] = 25.0m,
            ["profitSharePercentage"] = 20.0m
        });

        await HandleEvent("VaultAllocationUpdated", vaultId, participantId, new Dictionary<string, object>
        {
            ["vaultId"] = vaultId.ToString(),
            ["participantId"] = participantId.ToString(),
            ["allocationPercentage"] = 30.0m,
            ["profitSharePercentage"] = 25.0m
        });

        var model = await LoadModel(vaultId, participantId);
        Assert.NotNull(model);
        Assert.Equal(30.0m, model.AllocationPercentage);
        Assert.Equal(25.0m, model.ProfitSharePercentage);
    }

    [Fact]
    public async Task HandleAsync_ParticipantRemoved_DeletesRecord()
    {
        var vaultId = Guid.NewGuid();
        var participantId = Guid.NewGuid();

        await HandleEvent("VaultParticipantAdded", vaultId, participantId, new Dictionary<string, object>
        {
            ["vaultId"] = vaultId.ToString(),
            ["participantId"] = participantId.ToString()
        });

        var before = await LoadModel(vaultId, participantId);
        Assert.NotNull(before);

        await HandleEvent("VaultParticipantRemoved", vaultId, participantId, new Dictionary<string, object>
        {
            ["vaultId"] = vaultId.ToString(),
            ["participantId"] = participantId.ToString()
        });

        var after = await LoadModel(vaultId, participantId);
        Assert.Null(after);
    }

    [Fact]
    public async Task HandleAsync_DuplicateEvents_IdempotentProcessing()
    {
        var vaultId = Guid.NewGuid();
        var participantId = Guid.NewGuid();

        var payload = new Dictionary<string, object>
        {
            ["vaultId"] = vaultId.ToString(),
            ["participantId"] = participantId.ToString(),
            ["allocationPercentage"] = 25.0m,
            ["profitSharePercentage"] = 20.0m
        };

        await HandleEvent("VaultParticipantAdded", vaultId, participantId, new Dictionary<string, object>
        {
            ["vaultId"] = vaultId.ToString(),
            ["participantId"] = participantId.ToString()
        });

        await HandleEvent("VaultAllocationCreated", vaultId, participantId, payload);
        await HandleEvent("VaultAllocationCreated", vaultId, participantId, payload);

        var model = await LoadModel(vaultId, participantId);
        Assert.NotNull(model);
        Assert.Equal(25.0m, model.AllocationPercentage);
    }

    [Fact]
    public async Task HandleAsync_ProjectionReplay_RebuildsState()
    {
        var vaultId = Guid.NewGuid();
        var participantId = Guid.NewGuid();

        var events = new (string EventType, Dictionary<string, object> Payload)[]
        {
            ("VaultParticipantAdded", new Dictionary<string, object>
            {
                ["vaultId"] = vaultId.ToString(),
                ["participantId"] = participantId.ToString(),
                ["participantRole"] = "Investor"
            }),
            ("VaultAllocationCreated", new Dictionary<string, object>
            {
                ["vaultId"] = vaultId.ToString(),
                ["participantId"] = participantId.ToString(),
                ["allocationPercentage"] = 40.0m,
                ["profitSharePercentage"] = 35.0m
            }),
            ("VaultContributionRecorded", new Dictionary<string, object>
            {
                ["vaultId"] = vaultId.ToString(),
                ["participantId"] = participantId.ToString(),
                ["amount"] = 10000m
            })
        };

        foreach (var (eventType, payload) in events)
            await HandleEvent(eventType, vaultId, participantId, payload);

        var model = await LoadModel(vaultId, participantId);
        Assert.NotNull(model);
        Assert.Equal("Investor", model.ParticipantRole);
        Assert.Equal(40.0m, model.AllocationPercentage);
        Assert.Equal(35.0m, model.ProfitSharePercentage);
        Assert.Equal(10000m, model.ContributionAmount);
    }

    private async Task HandleEvent(string eventType, Guid vaultId, Guid participantId,
        Dictionary<string, object> payload)
    {
        var envelope = new EventEnvelope(
            Guid.NewGuid(),
            eventType,
            "whyce.economic.events",
            payload,
            new PartitionKey(vaultId.ToString()),
            Timestamp.Now());

        await _projection.HandleAsync(envelope);
    }

    private async Task<VaultParticipantAllocationReadModel?> LoadModel(Guid vaultId, Guid participantId)
    {
        var json = await _store.GetAsync($"vault-allocation:{vaultId}:{participantId}");
        return json is not null
            ? JsonSerializer.Deserialize<VaultParticipantAllocationReadModel>(json)
            : null;
    }
}
