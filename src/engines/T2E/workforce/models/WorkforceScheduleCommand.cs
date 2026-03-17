namespace Whycespace.Engines.T2E.Workforce.Models;

public sealed record WorkforceScheduleCommand(
    Guid WorkforceId,
    Guid OperatorId,
    Guid TaskId,
    string TaskType,
    DateTimeOffset ScheduleStart,
    DateTimeOffset ScheduleEnd,
    string ScheduleScope
);
