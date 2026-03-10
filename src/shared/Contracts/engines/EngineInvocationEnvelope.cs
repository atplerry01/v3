namespace Whycespace.Contracts.Engines;

public sealed record EngineInvocationEnvelope(
    Guid InvocationId,
    string EngineName,
    string WorkflowId,
    string WorkflowStep,
    string PartitionKey,
    IReadOnlyDictionary<string, object> Context
);
