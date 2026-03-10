namespace Whycespace.Runtime.Projections;

using Whycespace.Shared.Events;
using Whycespace.Shared.Projections;

public sealed class DriverLocationProjection : IProjection
{
    private readonly Dictionary<string, (double Lat, double Lon)> _locations = new();

    public string Name => "DriverLocation";

    public Task HandleAsync(SystemEvent @event)
    {
        if (@event.EventType == "DriverLocationUpdated")
        {
            var driverId = @event.Payload.GetValueOrDefault("driverId") as string;
            if (driverId is not null
                && @event.Payload.GetValueOrDefault("latitude") is double lat
                && @event.Payload.GetValueOrDefault("longitude") is double lon)
            {
                _locations[driverId] = (lat, lon);
            }
        }
        return Task.CompletedTask;
    }

    public IReadOnlyDictionary<string, (double Lat, double Lon)> GetLocations() => _locations;
}
