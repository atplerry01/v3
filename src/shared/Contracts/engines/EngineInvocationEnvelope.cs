namespace Whycespace.Contracts.Engines;

using Whycespace.Contracts.Primitives;

public sealed record EngineInvocationEnvelope(
    Guid InvocationId,
    string EngineName,
    string WorkflowId,
    string WorkflowStep,
    PartitionKey PartitionKey,
    IReadOnlyDictionary<string, object> Context
);
