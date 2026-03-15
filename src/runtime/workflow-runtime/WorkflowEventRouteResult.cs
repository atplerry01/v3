namespace Whycespace.WorkflowRuntime;

public sealed record WorkflowEventRouteResult(
    string WorkflowInstanceId,
    Guid EventId,
    string? AffectedStep,
    RoutingStatus RoutingStatus,
    DateTimeOffset RoutedAt
)
{
    public static WorkflowEventRouteResult Matched(
        string workflowInstanceId,
        Guid eventId,
        string affectedStep)
        => new(workflowInstanceId, eventId, affectedStep, RoutingStatus.Matched, DateTimeOffset.UtcNow);

    public static WorkflowEventRouteResult Ignored(Guid eventId)
        => new(string.Empty, eventId, null, RoutingStatus.Ignored, DateTimeOffset.UtcNow);

    public static WorkflowEventRouteResult Failed(Guid eventId, string workflowInstanceId = "")
        => new(workflowInstanceId, eventId, null, RoutingStatus.Failed, DateTimeOffset.UtcNow);
}

public enum RoutingStatus
{
    Matched,
    Ignored,
    Failed
}
