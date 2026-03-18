namespace Whycespace.Contracts.Runtime;

using Whycespace.Shared.Primitives.Common;

public sealed record WorkflowExecutionRequest(
    string WorkflowName,
    IReadOnlyDictionary<string, object> Context,
    string? CorrelationId = null,
    DateTimeOffset? ScheduledAt = null,
    PartitionKey PartitionKey = default
);
