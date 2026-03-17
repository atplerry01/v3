namespace Whycespace.Engines.T1M.WSS.Definition;

using Whycespace.Engines.T1M.WSS.Workflows;

public sealed record WorkflowDefinitionResult(
    string WorkflowId,
    string WorkflowName,
    string WorkflowVersion,
    IReadOnlyList<WorkflowStepDefinition> WorkflowSteps,
    IReadOnlyList<WorkflowParameterDefinition> WorkflowParameters,
    DateTimeOffset CreatedAt
);
