namespace Whycespace.Engines.T1M.WSS.Workflows;

public sealed record WorkflowInstance(
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
