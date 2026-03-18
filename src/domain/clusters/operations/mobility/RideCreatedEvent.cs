namespace Whycespace.Domain.Events.Clusters.Mobility;

public sealed record RideCreatedEvent(
    Guid RideId,
    Guid DriverId,
    Guid RiderId,
    string PartitionKey,
    DateTimeOffset Timestamp
);
