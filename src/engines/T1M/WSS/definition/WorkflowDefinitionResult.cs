namespace Whycespace.Engines.T1M.WSS.Definition;

using Whycespace.Domain.Core.Workflows;

public sealed record WorkflowDefinitionResult(
    string WorkflowId,
    string WorkflowName,
    string WorkflowVersion,
    IReadOnlyList<WorkflowStepDefinition> WorkflowSteps,
    IReadOnlyList<WorkflowParameterDefinition> WorkflowParameters,
    DateTimeOffset CreatedAt
);
