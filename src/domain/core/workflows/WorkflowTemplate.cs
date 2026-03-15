namespace Whycespace.Domain.Core.Workflows;

/// <summary>
/// Domain-level workflow template for parameterized workflow creation.
/// </summary>
public sealed record WorkflowTemplate(
    string TemplateId,
    string Name,
    string Description,
    int Version,
    IReadOnlyList<WorkflowStepDefinition> Steps,
    IReadOnlyList<WorkflowParameterDefinition> Parameters
);
