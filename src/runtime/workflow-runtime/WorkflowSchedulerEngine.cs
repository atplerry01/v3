namespace Whycespace.WorkflowRuntime;

/// <summary>
/// Runtime workflow scheduler. Manages workflow scheduling priorities.
/// Moved from engine layer to runtime layer as part of WBSM v3 architecture compliance.
/// </summary>
public sealed class WorkflowSchedulerEngine
{
    public WorkflowScheduleResult Schedule(string workflowName, string priority = "normal")
    {
        return new WorkflowScheduleResult(
            workflowName,
            priority,
            DateTimeOffset.UtcNow);
    }
}

public sealed record WorkflowScheduleResult(
    string WorkflowName,
    string Priority,
    DateTimeOffset ScheduledAt);
