namespace Whycespace.Tests.Projections;

using Whycespace.Runtime.Projections;
using Whycespace.Shared.Events;
using Xunit;

public sealed class DriverLocationProjectionTests
{
    [Fact]
    public async Task HandleAsync_DriverLocationUpdated_StoresLocation()
    {
        var projection = new DriverLocationProjection();
        var @event = new SystemEvent(
            Guid.NewGuid(), "DriverLocationUpdated", Guid.NewGuid(),
            DateTimeOffset.UtcNow, new Dictionary<string, object>
            {
                ["driverId"] = "driver-1",
                ["latitude"] = 51.5074,
                ["longitude"] = -0.1278
            });

        await projection.HandleAsync(@event);
        var locations = projection.GetLocations();

        Assert.Single(locations);
        Assert.Equal(51.5074, locations["driver-1"].Lat);
        Assert.Equal(-0.1278, locations["driver-1"].Lon);
    }
}
