namespace Whycespace.System.Midstream.WSS.Models;

public sealed record WorkflowInstance(
    string InstanceId,
    string WorkflowId,
    string WorkflowVersion,
    string CurrentStep,
    WorkflowInstanceStatus Status,
    DateTimeOffset StartedAt,
    DateTimeOffset? CompletedAt,
    IReadOnlyDictionary<string, object> Context
);
