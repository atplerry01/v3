namespace Whycespace.Contracts.Workflows;

using Whycespace.Shared.Primitives.Common;

public sealed record WorkflowContext(
    string WorkflowId,
    string WorkflowName,
    string CurrentStep,
    IReadOnlyDictionary<string, object> Data,
    DateTimeOffset StartedAt,
    PartitionKey PartitionKey = default
);
