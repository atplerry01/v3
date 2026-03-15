namespace Whycespace.Engines.T1M.WSS.Template;

using Whycespace.System.Midstream.WSS.Models;

public sealed record WorkflowTemplateResult(
    string TemplateId,
    string TemplateName,
    int TemplateVersion,
    IReadOnlyList<WorkflowTemplateCommandStep> TemplateSteps,
    IReadOnlyList<WorkflowTemplateParameter> TemplateParameters,
    DateTimeOffset CreatedAt
);
