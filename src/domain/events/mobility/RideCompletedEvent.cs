namespace Whycespace.Domain.Events.Mobility;

public sealed record RideCompletedEvent(
    Guid RideId,
    Guid DriverId,
    Guid RiderId,
    decimal Fare,
    DateTimeOffset Timestamp
);
