namespace Whycespace.System.Midstream.WSS.Events;

public static class WorkflowEventTypes
{
    public const string WorkflowStarted = "WorkflowStarted";
    public const string WorkflowStepStarted = "WorkflowStepStarted";
    public const string WorkflowStepCompleted = "WorkflowStepCompleted";
    public const string WorkflowStepFailed = "WorkflowStepFailed";
    public const string WorkflowCompleted = "WorkflowCompleted";
    public const string WorkflowCancelled = "WorkflowCancelled";

    public static IReadOnlyList<string> All { get; } = new[]
    {
        WorkflowStarted,
        WorkflowStepStarted,
        WorkflowStepCompleted,
        WorkflowStepFailed,
        WorkflowCompleted,
        WorkflowCancelled
    };
}
