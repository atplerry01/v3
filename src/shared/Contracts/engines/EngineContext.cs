namespace Whycespace.Contracts.Engines;

using Whycespace.Contracts.Primitives;

public sealed record EngineContext(
    Guid InvocationId,
    string WorkflowId,
    string WorkflowStep,
    PartitionKey PartitionKey,
    IReadOnlyDictionary<string, object> Data
);
