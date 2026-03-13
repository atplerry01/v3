namespace Whycespace.SystemValidation.Tests;

using Whycespace.Contracts.Engines;
using Whycespace.Engines.T2E.Clusters.Mobility.Taxi;
using Whycespace.Engines.T2E.Clusters.Property.Letting;

public sealed class ProjectionValidationTests
{
    [Fact]
    public async Task RideCompletion_ProducesProjectionEvent()
    {
        var engine = new RideExecutionEngine();
        var context = new EngineContext(
            Guid.NewGuid(), Guid.NewGuid().ToString(), "CompleteTrip",
            "partition-1", new Dictionary<string, object>
            {
                ["fare"] = 30.00m
            });

        var result = await engine.ExecuteAsync(context);

        Assert.True(result.Success);
        Assert.Equal("TripCompleted", result.Events[0].EventType);
    }

    [Fact]
    public async Task PropertyListing_ProducesProjectionEvent()
    {
        var engine = new PropertyExecutionEngine();
        var context = new EngineContext(
            Guid.NewGuid(), Guid.NewGuid().ToString(), "PublishListing",
            "partition-1", new Dictionary<string, object>
            {
                ["title"] = "3 Bed House",
                ["monthlyRent"] = 2000.00m
            });

        var result = await engine.ExecuteAsync(context);

        Assert.True(result.Success);
        Assert.Equal("ListingPublished", result.Events[0].EventType);
    }
}
