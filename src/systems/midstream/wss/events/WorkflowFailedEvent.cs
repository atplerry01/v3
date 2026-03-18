namespace Whycespace.Systems.Midstream.WSS.Events;

public sealed record WorkflowFailedEvent(
    Guid EventId,
    string WorkflowId,
    string InstanceId,
    string FailureReason,
    string? FailedStepId,
    DateTimeOffset FailedAt
)
{
    public static WorkflowFailedEvent Create(string workflowId, string instanceId, string reason, string? failedStepId = null)
        => new(Guid.NewGuid(), workflowId, instanceId, reason, failedStepId, DateTimeOffset.UtcNow);
}
