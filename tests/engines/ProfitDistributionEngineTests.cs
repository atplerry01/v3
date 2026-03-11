namespace Whycespace.Tests.Engines;

using Whycespace.Engines.T2E.Core.Capital;
using Whycespace.Contracts.Engines;
using Xunit;

public sealed class ProfitDistributionEngineTests
{
    private readonly ProfitDistributionEngine _engine = new();

    [Fact]
    public async Task ValidDistribution_Succeeds()
    {
        var context = new EngineContext(
            Guid.NewGuid(), Guid.NewGuid().ToString(), "Distribute",
            "partition-1", new Dictionary<string, object>
            {
                ["spvId"] = Guid.NewGuid().ToString(),
                ["vaultId"] = Guid.NewGuid().ToString(),
                ["totalRevenue"] = 100000m,
                ["totalCosts"] = 30000m
            });

        var result = await _engine.ExecuteAsync(context);
        Assert.True(result.Success);
        Assert.Equal(3, result.Events.Count);
        Assert.Contains(result.Events, e => e.EventType == "ProfitDistributed");
        Assert.Contains(result.Events, e => e.EventType == "VaultCredited");
        Assert.Contains(result.Events, e => e.EventType == "SpvProfitSettled");
    }

    [Fact]
    public async Task NoProfit_Fails()
    {
        var context = new EngineContext(
            Guid.NewGuid(), Guid.NewGuid().ToString(), "Distribute",
            "partition-1", new Dictionary<string, object>
            {
                ["spvId"] = Guid.NewGuid().ToString(),
                ["vaultId"] = Guid.NewGuid().ToString(),
                ["totalRevenue"] = 10000m,
                ["totalCosts"] = 15000m
            });

        var result = await _engine.ExecuteAsync(context);
        Assert.False(result.Success);
    }

    [Fact]
    public async Task CustomDistributionRate_DistributesCorrectly()
    {
        var context = new EngineContext(
            Guid.NewGuid(), Guid.NewGuid().ToString(), "Distribute",
            "partition-1", new Dictionary<string, object>
            {
                ["spvId"] = Guid.NewGuid().ToString(),
                ["vaultId"] = Guid.NewGuid().ToString(),
                ["totalRevenue"] = 100000m,
                ["totalCosts"] = 0m,
                ["distributionRate"] = 0.5m
            });

        var result = await _engine.ExecuteAsync(context);
        Assert.True(result.Success);
        Assert.Equal(50000m, result.Output["distributionAmount"]);
        Assert.Equal(50000m, result.Output["retainedAmount"]);
    }
}
