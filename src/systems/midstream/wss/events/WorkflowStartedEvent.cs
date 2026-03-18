namespace Whycespace.Systems.Midstream.WSS.Events;

public sealed record WorkflowStartedEvent(
    Guid EventId,
    string WorkflowId,
    string InstanceId,
    string WorkflowName,
    DateTimeOffset StartedAt,
    IReadOnlyDictionary<string, object> InitialContext
)
{
    public static WorkflowStartedEvent Create(string workflowId, string instanceId, string workflowName, IReadOnlyDictionary<string, object>? context = null)
        => new(Guid.NewGuid(), workflowId, instanceId, workflowName, DateTimeOffset.UtcNow, context ?? new Dictionary<string, object>());
}
