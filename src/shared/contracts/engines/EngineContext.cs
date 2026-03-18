namespace Whycespace.Contracts.Engines;

using Whycespace.Shared.Primitives.Common;

public sealed record EngineContext(
    Guid InvocationId,
    string WorkflowId,
    string WorkflowStep,
    PartitionKey PartitionKey,
    IReadOnlyDictionary<string, object> Data
);
