using Whycespace.Shared.Primitives.Common;

namespace Whycespace.Shared.Context;

public sealed record ExecutionContext(
    CorrelationId CorrelationId,
    string EngineName,
    string WorkflowId,
    string StepName,
    PartitionKey PartitionKey,
    DateTimeOffset StartedAt
);
