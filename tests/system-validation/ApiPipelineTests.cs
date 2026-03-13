namespace Whycespace.SystemValidation.Tests;

using Whycespace.Contracts.Engines;
using Whycespace.Engines.T4A.API;

public sealed class ApiPipelineTests
{
    [Fact]
    public async Task ApiEngine_AcceptsRideRequest_AndProducesCommandEvent()
    {
        var engine = new APIEngine();
        var context = new EngineContext(
            Guid.NewGuid(), Guid.NewGuid().ToString(), "DispatchCommand",
            "partition-1", new Dictionary<string, object>
            {
                ["apiAction"] = "ride.request",
                ["userId"] = "user-1",
                ["pickupLatitude"] = 51.5074
            });

        var result = await engine.ExecuteAsync(context);

        Assert.True(result.Success);
        Assert.Equal("APICommandAccepted", result.Events[0].EventType);
        Assert.Equal("RequestRide", result.Output["commandType"]);
    }

    [Fact]
    public async Task ApiEngine_AcceptsPropertyList_AndProducesCommandEvent()
    {
        var engine = new APIEngine();
        var context = new EngineContext(
            Guid.NewGuid(), Guid.NewGuid().ToString(), "DispatchCommand",
            "partition-1", new Dictionary<string, object>
            {
                ["apiAction"] = "property.list",
                ["userId"] = "owner-1",
                ["title"] = "Test Property"
            });

        var result = await engine.ExecuteAsync(context);

        Assert.True(result.Success);
        Assert.Equal("ListProperty", result.Output["commandType"]);
    }

    [Fact]
    public async Task ApiEngine_RoutesEconomicCommands_ToCorrectCommandType()
    {
        var engine = new APIEngine();
        var actions = new Dictionary<string, string>
        {
            ["vault.create"] = "CreateVault",
            ["spv.create"] = "CreateSpv",
            ["revenue.record"] = "RecordRevenue",
            ["profit.distribute"] = "DistributeProfit"
        };

        foreach (var (apiAction, expectedCommand) in actions)
        {
            var context = new EngineContext(
                Guid.NewGuid(), Guid.NewGuid().ToString(), "DispatchCommand",
                "partition-1", new Dictionary<string, object>
                {
                    ["apiAction"] = apiAction,
                    ["userId"] = "user-1"
                });

            var result = await engine.ExecuteAsync(context);
            Assert.True(result.Success, $"Failed for {apiAction}");
            Assert.Equal(expectedCommand, result.Output["commandType"]);
        }
    }
}
