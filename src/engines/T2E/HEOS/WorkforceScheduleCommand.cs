namespace Whycespace.Engines.T2E.HEOS;

public sealed record WorkforceScheduleCommand(
    Guid WorkforceId,
    Guid OperatorId,
    Guid TaskId,
    string TaskType,
    DateTimeOffset ScheduleStart,
    DateTimeOffset ScheduleEnd,
    string ScheduleScope
);
