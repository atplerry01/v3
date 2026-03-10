namespace Whycespace.Contracts.Runtime;

using Whycespace.Contracts.Primitives;

public sealed record WorkflowExecutionRequest(
    string WorkflowName,
    IReadOnlyDictionary<string, object> Context,
    string? CorrelationId = null,
    DateTimeOffset? ScheduledAt = null,
    PartitionKey PartitionKey = default
);
