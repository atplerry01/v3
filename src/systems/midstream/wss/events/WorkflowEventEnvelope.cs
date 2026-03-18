
namespace Whycespace.Systems.Midstream.WSS.Events;
using Whycespace.Shared.Envelopes;

public sealed record WorkflowEventEnvelope(
    Guid EventId,
    string EventType,
    string WorkflowId,
    string InstanceId,
    DateTimeOffset Timestamp,
    IDictionary<string, object> Payload
)
{
    public static WorkflowEventEnvelope Create(
        string eventType,
        string workflowId,
        string instanceId,
        IDictionary<string, object>? payload = null)
        => new(
            Guid.NewGuid(),
            eventType,
            workflowId,
            instanceId,
            DateTimeOffset.UtcNow,
            payload ?? new Dictionary<string, object>());
}
