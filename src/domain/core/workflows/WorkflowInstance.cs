namespace Whycespace.Domain.Core.Workflows;

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
