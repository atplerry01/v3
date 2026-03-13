using Whycespace.Contracts.Primitives;
using Whycespace.EventFabric.Models;
using Whycespace.Projections.Projections;
using Whycespace.Projections.Storage;

namespace Whycespace.Projections.Tests;

public sealed class VaultBalanceProjectionTests
{
    [Fact]
    public async Task HandleAsync_CapitalContribution_IncreasesBalance()
    {
        var store = new RedisProjectionStore();
        var projection = new VaultBalanceProjection(store);

        var vaultId = Guid.NewGuid().ToString();
        var envelope = new EventEnvelope(
            Guid.NewGuid(),
            "CapitalContributionRecordedEvent",
            "whyce.economic.events",
            new Dictionary<string, object>
            {
                ["vaultId"] = vaultId,
                ["amount"] = 5000m
            },
            new PartitionKey(vaultId),
            Timestamp.Now());

        await projection.HandleAsync(envelope);

        var result = await store.GetAsync($"vault:{vaultId}");
        Assert.NotNull(result);
        Assert.Contains("5000", result);
    }

    [Fact]
    public async Task HandleAsync_ProfitDistributed_IncreasesBalance()
    {
        var store = new RedisProjectionStore();
        var projection = new VaultBalanceProjection(store);

        var vaultId = Guid.NewGuid().ToString();
        var envelope = new EventEnvelope(
            Guid.NewGuid(),
            "ProfitDistributedEvent",
            "whyce.economic.events",
            new Dictionary<string, object>
            {
                ["vaultId"] = vaultId,
                ["amount"] = 3000m
            },
            new PartitionKey(vaultId),
            Timestamp.Now());

        await projection.HandleAsync(envelope);

        var result = await store.GetAsync($"vault:{vaultId}");
        Assert.NotNull(result);
        Assert.Contains("3000", result);
    }
}
