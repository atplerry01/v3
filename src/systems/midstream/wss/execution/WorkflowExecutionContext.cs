namespace Whycespace.Systems.Midstream.WSS.Execution;

public sealed record WorkflowExecutionContext(
    string WorkflowId,
    string WorkflowName,
    string CurrentStep,
    IReadOnlyDictionary<string, object> Data,
    DateTimeOffset StartedAt
);
