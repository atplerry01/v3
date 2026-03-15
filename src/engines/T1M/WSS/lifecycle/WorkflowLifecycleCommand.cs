namespace Whycespace.Engines.T1M.WSS.Lifecycle;

public sealed record WorkflowLifecycleCommand(
    string WorkflowInstanceId,
    WorkflowLifecycleStatus CurrentStatus,
    WorkflowLifecycleTransition RequestedTransition,
    DateTimeOffset Timestamp
)
{
    public static WorkflowLifecycleCommand FromContextData(IReadOnlyDictionary<string, object> data)
    {
        var instanceId = data.GetValueOrDefault("workflowInstanceId") as string ?? string.Empty;

        var currentStatus = ParseStatus(data.GetValueOrDefault("currentStatus"));
        var requestedTransition = ParseTransition(data.GetValueOrDefault("requestedTransition"));

        var timestamp = data.TryGetValue("timestamp", out var ts) && ts is DateTimeOffset dto
            ? dto
            : DateTimeOffset.UtcNow;

        return new WorkflowLifecycleCommand(instanceId, currentStatus, requestedTransition, timestamp);
    }

    private static WorkflowLifecycleStatus ParseStatus(object? value)
    {
        if (value is WorkflowLifecycleStatus status)
            return status;

        if (value is string str && Enum.TryParse<WorkflowLifecycleStatus>(str, ignoreCase: true, out var parsed))
            return parsed;

        return WorkflowLifecycleStatus.Created;
    }

    private static WorkflowLifecycleTransition ParseTransition(object? value)
    {
        if (value is WorkflowLifecycleTransition transition)
            return transition;

        if (value is string str && Enum.TryParse<WorkflowLifecycleTransition>(str, ignoreCase: true, out var parsed))
            return parsed;

        return WorkflowLifecycleTransition.Create;
    }
}

public enum WorkflowLifecycleStatus
{
    Created = 0,
    Running = 1,
    Waiting = 2,
    Retrying = 3,
    Completed = 4,
    Failed = 5,
    Terminated = 6
}

public enum WorkflowLifecycleTransition
{
    Create = 0,
    Start = 1,
    Complete = 2,
    Fail = 3,
    Terminate = 4,
    Recover = 5
}
