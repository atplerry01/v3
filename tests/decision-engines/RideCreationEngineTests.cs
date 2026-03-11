namespace Whycespace.DecisionEngines.Tests;

using Whycespace.Engines.T3I.Clusters.Mobility.Taxi;
using Whycespace.Contracts.Engines;

public sealed class RideCreationEngineTests
{
    private readonly RideCreationEngine _engine = new();

    [Fact]
    public async Task ExecutesSuccessfully_WithValidInput()
    {
        var context = new EngineContext(
            Guid.NewGuid(), Guid.NewGuid().ToString(), "CreateRide",
            "partition-1", new Dictionary<string, object>
            {
                ["riderId"] = Guid.NewGuid().ToString(),
                ["assignedDriverId"] = Guid.NewGuid().ToString()
            });

        var result = await _engine.ExecuteAsync(context);

        Assert.True(result.Success);
        Assert.Single(result.Events);
        Assert.Equal("RideCreated", result.Events[0].EventType);
        Assert.True(result.Output.ContainsKey("rideId"));
    }

    [Fact]
    public async Task EmitsEvent_WithCorrectPayload()
    {
        var riderId = Guid.NewGuid().ToString();
        var driverId = Guid.NewGuid().ToString();

        var context = new EngineContext(
            Guid.NewGuid(), Guid.NewGuid().ToString(), "CreateRide",
            "partition-1", new Dictionary<string, object>
            {
                ["riderId"] = riderId,
                ["assignedDriverId"] = driverId
            });

        var result = await _engine.ExecuteAsync(context);

        var evt = result.Events[0];
        Assert.Equal("RideCreated", evt.EventType);
        Assert.Equal(riderId, evt.Payload["riderId"]);
        Assert.Equal(driverId, evt.Payload["driverId"]);
        Assert.Equal("whyce.mobility.events", evt.Payload["topic"]);
    }

    [Fact]
    public async Task Deterministic_SameStructureOnReplay()
    {
        var context = new EngineContext(
            Guid.NewGuid(), Guid.NewGuid().ToString(), "CreateRide",
            "partition-1", new Dictionary<string, object>
            {
                ["riderId"] = Guid.NewGuid().ToString(),
                ["assignedDriverId"] = Guid.NewGuid().ToString()
            });

        var result1 = await _engine.ExecuteAsync(context);
        var result2 = await _engine.ExecuteAsync(context);

        Assert.True(result1.Success);
        Assert.True(result2.Success);
        Assert.Equal(result1.Events[0].EventType, result2.Events[0].EventType);
    }

    [Fact]
    public async Task MissingRiderId_Fails()
    {
        var context = new EngineContext(
            Guid.NewGuid(), Guid.NewGuid().ToString(), "CreateRide",
            "partition-1", new Dictionary<string, object>
            {
                ["assignedDriverId"] = Guid.NewGuid().ToString()
            });

        var result = await _engine.ExecuteAsync(context);

        Assert.False(result.Success);
    }
}
