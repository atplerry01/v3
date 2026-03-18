namespace Whycespace.EconomicRuntime.Tests;

using Whycespace.Contracts.Engines;
using Whycespace.Contracts.Primitives;
using Whycespace.Engines.T2E.Economic.Revenue.Engines;

public sealed class RevenueRecordingEngineTests
{
    [Fact]
    public async Task RecordRevenue_ProducesEvent()
    {
        var engine = new RevenueRecordingEngine();
        var spvId = Guid.NewGuid();
        var assetId = Guid.NewGuid();
        var context = new EngineContext(
            Guid.NewGuid(), spvId.ToString(), "RecordRevenue",
            new PartitionKey("whyce.economic"),
            new Dictionary<string, object>
            {
                ["spvId"] = spvId.ToString(),
                ["assetId"] = assetId.ToString(),
                ["amount"] = 1500m,
                ["source"] = "TaxiRide"
            });

        var result = await engine.ExecuteAsync(context);

        Assert.True(result.Success);
        Assert.Equal(2, result.Events.Count);
        Assert.Equal("RevenueRecorded", result.Events[0].EventType);
    }
}
