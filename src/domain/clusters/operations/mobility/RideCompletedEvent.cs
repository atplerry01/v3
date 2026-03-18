namespace Whycespace.Domain.Clusters.Operations.Mobility;

public sealed record RideCompletedEvent(
    Guid RideId,
    Guid DriverId,
    Guid RiderId,
    decimal Fare,
    DateTimeOffset Timestamp
);
