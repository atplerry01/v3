namespace Whycespace.DecisionEngines.Tests;

using Whycespace.Engines.T3I_Intelligence;
using Whycespace.Contracts.Engines;

public sealed class DriverMatchingEngineTests
{
    private readonly DriverMatchingEngine _engine = new();

    [Fact]
    public async Task ExecutesSuccessfully_WithValidCoordinates()
    {
        var context = new EngineContext(
            Guid.NewGuid(), Guid.NewGuid().ToString(), "MatchDriver",
            "partition-1", new Dictionary<string, object>
            {
                ["pickupLatitude"] = 51.5074,
                ["pickupLongitude"] = -0.1278
            });

        var result = await _engine.ExecuteAsync(context);

        Assert.True(result.Success);
        Assert.Single(result.Events);
        Assert.Equal("DriverMatched", result.Events[0].EventType);
        Assert.True(result.Output.ContainsKey("assignedDriverId"));
    }

    [Fact]
    public async Task EmitsEvent_WithPickupCoordinates()
    {
        var context = new EngineContext(
            Guid.NewGuid(), Guid.NewGuid().ToString(), "MatchDriver",
            "partition-1", new Dictionary<string, object>
            {
                ["pickupLatitude"] = 40.7128,
                ["pickupLongitude"] = -74.0060
            });

        var result = await _engine.ExecuteAsync(context);

        var evt = result.Events[0];
        Assert.Equal("DriverMatched", evt.EventType);
        Assert.Equal(40.7128, evt.Payload["pickupLatitude"]);
        Assert.Equal(-74.0060, evt.Payload["pickupLongitude"]);
    }

    [Fact]
    public async Task Deterministic_SameStructureOnReplay()
    {
        var context = new EngineContext(
            Guid.NewGuid(), Guid.NewGuid().ToString(), "MatchDriver",
            "partition-1", new Dictionary<string, object>
            {
                ["pickupLatitude"] = 51.5074,
                ["pickupLongitude"] = -0.1278
            });

        var result1 = await _engine.ExecuteAsync(context);
        var result2 = await _engine.ExecuteAsync(context);

        Assert.True(result1.Success);
        Assert.True(result2.Success);
        Assert.Equal(result1.Events.Count, result2.Events.Count);
        Assert.Equal(result1.Events[0].EventType, result2.Events[0].EventType);
    }

    [Fact]
    public async Task MissingCoordinates_Fails()
    {
        var context = new EngineContext(
            Guid.NewGuid(), Guid.NewGuid().ToString(), "MatchDriver",
            "partition-1", new Dictionary<string, object>());

        var result = await _engine.ExecuteAsync(context);

        Assert.False(result.Success);
    }
}
