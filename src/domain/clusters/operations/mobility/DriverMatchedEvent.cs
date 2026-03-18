namespace Whycespace.Domain.Events.Clusters.Mobility;

public sealed record DriverMatchedEvent(
    Guid MatchId,
    Guid WorkflowId,
    Guid DriverId,
    Guid RiderId,
    DateTimeOffset Timestamp
);
