namespace Whycespace.Tests.Engines;

using Whycespace.Engines.T2E.Economic.Revenue.Engines;
using Whycespace.Contracts.Engines;
using Xunit;

public sealed class RevenueRecordingEngineTests
{
    private readonly RevenueRecordingEngine _engine = new();

    [Fact]
    public async Task ValidRevenue_RecordsSuccessfully()
    {
        var context = new EngineContext(
            Guid.NewGuid(), Guid.NewGuid().ToString(), "Record",
            "partition-1", new Dictionary<string, object>
            {
                ["spvId"] = Guid.NewGuid().ToString(),
                ["assetId"] = Guid.NewGuid().ToString(),
                ["amount"] = 5000m,
                ["source"] = "Fare"
            });

        var result = await _engine.ExecuteAsync(context);
        Assert.True(result.Success);
        Assert.Equal(2, result.Events.Count);
        Assert.Contains(result.Events, e => e.EventType == "RevenueRecorded");
        Assert.Contains(result.Events, e => e.EventType == "SpvRevenueUpdated");
    }

    [Fact]
    public async Task MissingSource_Fails()
    {
        var context = new EngineContext(
            Guid.NewGuid(), Guid.NewGuid().ToString(), "Record",
            "partition-1", new Dictionary<string, object>
            {
                ["spvId"] = Guid.NewGuid().ToString(),
                ["assetId"] = Guid.NewGuid().ToString(),
                ["amount"] = 5000m
            });

        var result = await _engine.ExecuteAsync(context);
        Assert.False(result.Success);
    }

    [Fact]
    public async Task ZeroAmount_Fails()
    {
        var context = new EngineContext(
            Guid.NewGuid(), Guid.NewGuid().ToString(), "Record",
            "partition-1", new Dictionary<string, object>
            {
                ["spvId"] = Guid.NewGuid().ToString(),
                ["assetId"] = Guid.NewGuid().ToString(),
                ["amount"] = 0m,
                ["source"] = "Fare"
            });

        var result = await _engine.ExecuteAsync(context);
        Assert.False(result.Success);
    }
}
