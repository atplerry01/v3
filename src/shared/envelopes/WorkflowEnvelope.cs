namespace Whycespace.Shared.Envelopes;

public sealed record WorkflowEnvelope(
    Guid EnvelopeId,
    string WorkflowId,
    string InstanceId,
    string StepName,
    DateTimeOffset Timestamp,
    IReadOnlyDictionary<string, object> Payload
);
