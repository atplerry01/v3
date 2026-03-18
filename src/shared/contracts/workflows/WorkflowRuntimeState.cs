namespace Whycespace.Contracts.Workflows;

public sealed record WorkflowRuntimeState(
    Guid InstanceId,
    string CurrentNode,
    IReadOnlyDictionary<string, object> ContextData,
    DateTimeOffset UpdatedAt
);
