namespace Whycespace.EconomicRuntime.Tests;

using Whycespace.Contracts.Engines;
using Whycespace.Contracts.Primitives;
using Whycespace.Engines.T2E_Execution;

public sealed class ProfitDistributionEngineTests
{
    [Fact]
    public async Task DistributeProfit_ProducesEvent()
    {
        var engine = new ProfitDistributionEngine();
        var spvId = Guid.NewGuid();
        var vaultId = Guid.NewGuid();
        var context = new EngineContext(
            Guid.NewGuid(), spvId.ToString(), "DistributeProfit",
            new PartitionKey("whyce.economic"),
            new Dictionary<string, object>
            {
                ["spvId"] = spvId.ToString(),
                ["vaultId"] = vaultId.ToString(),
                ["totalRevenue"] = 10000m,
                ["totalCosts"] = 2500m
            });

        var result = await engine.ExecuteAsync(context);

        Assert.True(result.Success);
        Assert.Equal(3, result.Events.Count);
        Assert.Equal("ProfitDistributed", result.Events[0].EventType);
    }
}
