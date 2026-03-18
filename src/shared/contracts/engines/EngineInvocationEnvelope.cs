namespace Whycespace.Contracts.Engines;

using Whycespace.Shared.Primitives.Common;

public sealed record EngineInvocationEnvelope(
    Guid InvocationId,
    string EngineName,
    string WorkflowId,
    string WorkflowStep,
    PartitionKey PartitionKey,
    IReadOnlyDictionary<string, object> Context
);
