namespace Whycespace.Domain.Clusters.Operations.Mobility;

using Whycespace.Shared.Location;

public sealed record Ride(
    Guid RideId,
    Guid PassengerId,
    Guid? DriverId,
    GeoLocation PickupLocation,
    GeoLocation DropoffLocation,
    RideStatus Status,
    decimal? Fare,
    DateTimeOffset RequestedAt,
    DateTimeOffset? CompletedAt
);

public enum RideStatus
{
    Requested,
    DriverAssigned,
    InProgress,
    Completed,
    Cancelled
}
