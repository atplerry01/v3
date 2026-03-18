namespace Whycespace.Domain.Clusters.Operations.Mobility;

public sealed record RideCreatedEvent(
    Guid RideId,
    Guid DriverId,
    Guid RiderId,
    string PartitionKey,
    DateTimeOffset Timestamp
);
