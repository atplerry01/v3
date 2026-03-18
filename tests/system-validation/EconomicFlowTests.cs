namespace Whycespace.SystemValidation.Tests;

using Whycespace.Contracts.Engines;
using Whycespace.Engines.T2E.Economic.Distribution.Execution.Engines;
using Whycespace.Engines.T2E.Economic.Revenue.Recording.Engines;
using Whycespace.Engines.T2E.Economic.Vault.Creation.Engines;

public sealed class EconomicFlowTests
{
    [Fact]
    public async Task RevenueRecording_ProducesRevenueAndSpvEvents()
    {
        var engine = new RevenueRecordingEngine();
        var context = new EngineContext(
            Guid.NewGuid(), Guid.NewGuid().ToString(), "RecordRevenue",
            "partition-1", new Dictionary<string, object>
            {
                ["spvId"] = Guid.NewGuid().ToString(),
                ["assetId"] = Guid.NewGuid().ToString(),
                ["amount"] = 500.00m,
                ["source"] = "TaxiRide",
                ["period"] = "2026-Q1"
            });

        var result = await engine.ExecuteAsync(context);

        Assert.True(result.Success);
        Assert.Equal(2, result.Events.Count);
        Assert.Equal("RevenueRecorded", result.Events[0].EventType);
        Assert.Equal("SpvRevenueUpdated", result.Events[1].EventType);
    }

    [Fact]
    public async Task ProfitDistribution_CalculatesAndDistributesCorrectly()
    {
        var engine = new ProfitDistributionEngine();
        var context = new EngineContext(
            Guid.NewGuid(), Guid.NewGuid().ToString(), "DistributeProfit",
            "partition-1", new Dictionary<string, object>
            {
                ["spvId"] = Guid.NewGuid().ToString(),
                ["vaultId"] = Guid.NewGuid().ToString(),
                ["totalRevenue"] = 1000.00m,
                ["totalCosts"] = 400.00m,
                ["distributionRate"] = 0.7m
            });

        var result = await engine.ExecuteAsync(context);

        Assert.True(result.Success);
        Assert.Equal(3, result.Events.Count);
        Assert.Equal("ProfitDistributed", result.Events[0].EventType);
        Assert.Equal("VaultCredited", result.Events[1].EventType);
        Assert.Equal("SpvProfitSettled", result.Events[2].EventType);

        // Net profit = 1000 - 400 = 600, distribution = 600 * 0.7 = 420
        Assert.Equal(420.0m, result.Output["distributionAmount"]);
        Assert.Equal(180.0m, result.Output["retainedAmount"]);
    }

    [Fact]
    public async Task FullEconomicPipeline_VaultToProfit()
    {
        var workflowId = Guid.NewGuid().ToString();
        var spvId = Guid.NewGuid().ToString();
        var vaultId = Guid.NewGuid().ToString();
        var assetId = Guid.NewGuid().ToString();

        // Step 1: Create Vault
        var vaultEngine = new VaultCreationEngine();
        var vaultResult = await vaultEngine.ExecuteAsync(new EngineContext(
            Guid.NewGuid(), workflowId, "CreateVault",
            "partition-1", new Dictionary<string, object>
            {
                ["ownerId"] = Guid.NewGuid().ToString(),
                ["currency"] = "GBP",
                ["initialBalance"] = 10000.00m
            }));
        Assert.True(vaultResult.Success);

        // Step 2: Record Revenue
        var revenueEngine = new RevenueRecordingEngine();
        var revenueResult = await revenueEngine.ExecuteAsync(new EngineContext(
            Guid.NewGuid(), workflowId, "RecordRevenue",
            "partition-1", new Dictionary<string, object>
            {
                ["spvId"] = spvId,
                ["assetId"] = assetId,
                ["amount"] = 2000.00m,
                ["source"] = "PropertyLetting",
                ["period"] = "2026-Q1"
            }));
        Assert.True(revenueResult.Success);

        // Step 3: Distribute Profit
        var profitEngine = new ProfitDistributionEngine();
        var profitResult = await profitEngine.ExecuteAsync(new EngineContext(
            Guid.NewGuid(), workflowId, "DistributeProfit",
            "partition-1", new Dictionary<string, object>
            {
                ["spvId"] = spvId,
                ["vaultId"] = vaultId,
                ["totalRevenue"] = 2000.00m,
                ["totalCosts"] = 500.00m,
                ["distributionRate"] = 0.8m
            }));
        Assert.True(profitResult.Success);
        Assert.Equal(1200.0m, profitResult.Output["distributionAmount"]);
    }
}
