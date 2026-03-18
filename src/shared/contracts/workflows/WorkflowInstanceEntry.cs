namespace Whycespace.Contracts.Workflows;

public sealed record WorkflowInstanceEntry(
    Guid InstanceId,
    string WorkflowId,
    string CurrentStep,
    WorkflowStatus Status,
    DateTimeOffset StartedAt
);
