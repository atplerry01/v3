namespace Whycespace.Contracts.Workflows;

public sealed record WorkflowContext(
    string WorkflowId,
    string WorkflowName,
    string CurrentStep,
    IReadOnlyDictionary<string, object> Data,
    DateTimeOffset StartedAt
);
