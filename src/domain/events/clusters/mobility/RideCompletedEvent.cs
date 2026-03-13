namespace Whycespace.Domain.Events.Clusters.Mobility;

public sealed record RideCompletedEvent(
    Guid RideId,
    Guid DriverId,
    Guid RiderId,
    decimal Fare,
    DateTimeOffset Timestamp
);
