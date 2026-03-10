namespace Whycespace.Contracts.Runtime;

public sealed record WorkflowExecutionRequest(
    string WorkflowName,
    IReadOnlyDictionary<string, object> Context,
    string? CorrelationId = null,
    DateTimeOffset? ScheduledAt = null
);
