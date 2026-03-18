namespace Whycespace.Systems.Midstream.WSS.Events;

public sealed record WorkflowCompletedEvent(
    Guid EventId,
    string WorkflowId,
    string InstanceId,
    DateTimeOffset CompletedAt,
    IReadOnlyDictionary<string, object> Output
)
{
    public static WorkflowCompletedEvent Create(string workflowId, string instanceId, IReadOnlyDictionary<string, object>? output = null)
        => new(Guid.NewGuid(), workflowId, instanceId, DateTimeOffset.UtcNow, output ?? new Dictionary<string, object>());
}
