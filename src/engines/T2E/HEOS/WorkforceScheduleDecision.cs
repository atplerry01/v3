namespace Whycespace.Engines.T2E.HEOS;

public sealed record WorkforceScheduleDecision(
    bool Scheduled,
    string Reason,
    Guid WorkforceId,
    Guid TaskId,
    DateTimeOffset ScheduleStart,
    DateTimeOffset ScheduleEnd
)
{
    public static WorkforceScheduleDecision Success(
        Guid workforceId, Guid taskId,
        DateTimeOffset start, DateTimeOffset end)
        => new(true, "Workforce scheduled successfully", workforceId, taskId, start, end);

    public static WorkforceScheduleDecision Rejected(
        Guid workforceId, Guid taskId,
        DateTimeOffset start, DateTimeOffset end,
        string reason)
        => new(false, reason, workforceId, taskId, start, end);
}
