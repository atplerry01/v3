namespace Whycespace.Domain.Clusters.Operations.Mobility;

using Whycespace.Shared.Location;

public sealed record Driver(
    Guid DriverId,
    string Name,
    GeoLocation CurrentLocation,
    DriverStatus Status
);

public enum DriverStatus
{
    Available,
    OnTrip,
    Offline
}
