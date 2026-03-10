namespace Whycespace.Domain.Events.Mobility;

public sealed record DriverMatchedEvent(
    Guid MatchId,
    Guid WorkflowId,
    Guid DriverId,
    Guid RiderId,
    DateTimeOffset Timestamp
);
