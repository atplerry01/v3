namespace Whycespace.Tests.Engines;

using Whycespace.Engines.T3I_Intelligence;
using Whycespace.Shared.Contracts;
using Xunit;

public sealed class DriverMatchingEngineTests
{
    private readonly DriverMatchingEngine _engine = new();

    [Fact]
    public async Task ExecuteAsync_WithValidCoordinates_MatchesDriver()
    {
        var context = new EngineContext(
            Guid.NewGuid(), Guid.NewGuid().ToString(), "match",
            "partition-1", new Dictionary<string, object>
            {
                ["pickupLatitude"] = 51.5074,
                ["pickupLongitude"] = -0.1278
            });

        var result = await _engine.ExecuteAsync(context);
        Assert.True(result.Success);
        Assert.Contains("assignedDriverId", result.Output.Keys);
    }

    [Fact]
    public async Task ExecuteAsync_WithoutCoordinates_Fails()
    {
        var context = new EngineContext(
            Guid.NewGuid(), Guid.NewGuid().ToString(), "match",
            "partition-1", new Dictionary<string, object>());

        var result = await _engine.ExecuteAsync(context);
        Assert.False(result.Success);
    }
}
