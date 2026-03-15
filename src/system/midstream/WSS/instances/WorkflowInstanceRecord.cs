namespace Whycespace.System.Midstream.WSS.Instances;

using Whycespace.System.Midstream.WSS.Models;

public sealed record WorkflowInstanceRecord(
    string InstanceId,
    string WorkflowId,
    string WorkflowName,
    string WorkflowVersion,
    WorkflowInstanceStatus Status,
    DateTimeOffset StartedAt,
    DateTimeOffset? CompletedAt,
    string CorrelationId,
    string InitiatedBy
);
