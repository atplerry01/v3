namespace Whycespace.Domain.Clusters.Operations.Shared;

public sealed record ScheduleRecord(
    Guid TaskId,
    DateTimeOffset Start,
    DateTimeOffset End
);
