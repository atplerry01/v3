using Whycespace.Contracts.Primitives;
using Whycespace.EventFabric.Models;
using Whycespace.Projections.Projections;
using Whycespace.Projections.Storage;

namespace Whycespace.Projections.Tests;

public sealed class DriverLocationProjectionTests
{
    [Fact]
    public async Task HandleAsync_DriverLocationUpdatedEvent_StoresLocation()
    {
        var store = new RedisProjectionStore();
        var projection = new DriverLocationProjection(store);

        var envelope = new EventEnvelope(
            Guid.NewGuid(),
            "DriverLocationUpdatedEvent",
            "whyce.workflow.events",
            new Dictionary<string, object>
            {
                ["driverId"] = "driver-1",
                ["latitude"] = 51.5074,
                ["longitude"] = -0.1278
            },
            new PartitionKey("driver-1"),
            Timestamp.Now());

        await projection.HandleAsync(envelope);

        var result = await store.GetAsync("driver:driver-1");
        Assert.NotNull(result);
        Assert.Contains("51.5074", result);
        Assert.Contains("-0.1278", result);
    }
}
