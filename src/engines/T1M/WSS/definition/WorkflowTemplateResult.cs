namespace Whycespace.Engines.T1M.WSS.Definition;

using Whycespace.Systems.Midstream.WSS.Models;

public sealed record WorkflowTemplateResult(
    string TemplateId,
    string TemplateName,
    int TemplateVersion,
    IReadOnlyList<WorkflowTemplateCommandStep> TemplateSteps,
    IReadOnlyList<WorkflowTemplateParameter> TemplateParameters,
    DateTimeOffset CreatedAt
);
