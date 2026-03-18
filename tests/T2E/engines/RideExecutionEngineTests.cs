namespace Whycespace.Tests.Engines;

using Whycespace.Engines.T2E.Clusters.Mobility.Taxi.Engines;
using Whycespace.Contracts.Engines;
using Xunit;

public sealed class RideExecutionEngineTests
{
    private readonly RideExecutionEngine _engine = new();

    [Fact]
    public async Task ValidateRequest_WithPickupLocation_Succeeds()
    {
        var context = new EngineContext(
            Guid.NewGuid(), Guid.NewGuid().ToString(), "ValidateRequest",
            "partition-1", new Dictionary<string, object>
            {
                ["pickupLatitude"] = 51.5074
            });

        var result = await _engine.ExecuteAsync(context);
        Assert.True(result.Success);
    }

    [Fact]
    public async Task ValidateRequest_WithoutPickupLocation_Fails()
    {
        var context = new EngineContext(
            Guid.NewGuid(), Guid.NewGuid().ToString(), "ValidateRequest",
            "partition-1", new Dictionary<string, object>());

        var result = await _engine.ExecuteAsync(context);
        Assert.False(result.Success);
    }

    [Fact]
    public async Task AssignDriver_WithDriverId_Succeeds()
    {
        var context = new EngineContext(
            Guid.NewGuid(), Guid.NewGuid().ToString(), "AssignDriver",
            "partition-1", new Dictionary<string, object>
            {
                ["assignedDriverId"] = "driver-1"
            });

        var result = await _engine.ExecuteAsync(context);
        Assert.True(result.Success);
    }

    [Fact]
    public async Task UnknownStep_Fails()
    {
        var context = new EngineContext(
            Guid.NewGuid(), Guid.NewGuid().ToString(), "Unknown",
            "partition-1", new Dictionary<string, object>());

        var result = await _engine.ExecuteAsync(context);
        Assert.False(result.Success);
    }
}
