namespace Whycespace.Engines.T1M.WSS.Definition;

using Whycespace.Systems.Midstream.WSS.Models;
using Whycespace.Systems.Midstream.WSS.Definition;
using Whycespace.Systems.Midstream.WSS.Execution;
using Whycespace.Systems.Midstream.WSS.Policies;

public sealed record WorkflowTemplateResult(
    string TemplateId,
    string TemplateName,
    int TemplateVersion,
    IReadOnlyList<WorkflowTemplateCommandStep> TemplateSteps,
    IReadOnlyList<WorkflowTemplateParameter> TemplateParameters,
    DateTimeOffset CreatedAt
);
