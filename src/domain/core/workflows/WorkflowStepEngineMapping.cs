namespace Whycespace.Domain.Core.Workflows;

public sealed record WorkflowStepEngineMapping(
    string WorkflowId,
    string StepId,
    string EngineName,
    DateTimeOffset ResolvedAt
);
