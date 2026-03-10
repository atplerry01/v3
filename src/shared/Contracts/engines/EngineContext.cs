namespace Whycespace.Contracts.Engines;

public sealed record EngineContext(
    Guid InvocationId,
    string WorkflowId,
    string WorkflowStep,
    string PartitionKey,
    IReadOnlyDictionary<string, object> Data
);
