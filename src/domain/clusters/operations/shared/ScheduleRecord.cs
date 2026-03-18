namespace Whycespace.Domain.Core.Workforce;

public sealed record ScheduleRecord(
    Guid TaskId,
    DateTimeOffset Start,
    DateTimeOffset End
);
