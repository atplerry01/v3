namespace Whycespace.AccessEngines.Tests;

using Whycespace.Engines.T4A_Access;
using Whycespace.Contracts.Engines;

public sealed class ApiEngineTests
{
    private readonly APIEngine _engine = new();

    [Fact]
    public async Task DispatchesCommand_WithValidApiRequest()
    {
        var context = new EngineContext(
            Guid.NewGuid(), Guid.NewGuid().ToString(), "DispatchCommand",
            "partition-1", new Dictionary<string, object>
            {
                ["apiAction"] = "ride.request",
                ["userId"] = "user-123",
                ["pickupLatitude"] = 51.5074,
                ["pickupLongitude"] = -0.1278
            });

        var result = await _engine.ExecuteAsync(context);

        Assert.True(result.Success);
        Assert.Single(result.Events);
        Assert.Equal("APICommandAccepted", result.Events[0].EventType);
        Assert.Equal("RequestRide", result.Output["commandType"]);
        Assert.Equal(true, result.Output["accepted"]);
    }

    [Fact]
    public async Task RejectsRequest_WhenMissingUserId()
    {
        var context = new EngineContext(
            Guid.NewGuid(), Guid.NewGuid().ToString(), "DispatchCommand",
            "partition-1", new Dictionary<string, object>
            {
                ["apiAction"] = "ride.request"
            });

        var result = await _engine.ExecuteAsync(context);

        Assert.False(result.Success);
    }

    [Fact]
    public async Task RejectsRequest_WhenUnknownApiAction()
    {
        var context = new EngineContext(
            Guid.NewGuid(), Guid.NewGuid().ToString(), "DispatchCommand",
            "partition-1", new Dictionary<string, object>
            {
                ["apiAction"] = "unknown.action",
                ["userId"] = "user-123"
            });

        var result = await _engine.ExecuteAsync(context);

        Assert.False(result.Success);
    }

    [Fact]
    public async Task RoutesEconomicCommand_Successfully()
    {
        var context = new EngineContext(
            Guid.NewGuid(), Guid.NewGuid().ToString(), "DispatchCommand",
            "partition-1", new Dictionary<string, object>
            {
                ["apiAction"] = "spv.create",
                ["userId"] = "operator-1",
                ["spvName"] = "TestSPV"
            });

        var result = await _engine.ExecuteAsync(context);

        Assert.True(result.Success);
        Assert.Equal("CreateSpv", result.Output["commandType"]);
    }
}
