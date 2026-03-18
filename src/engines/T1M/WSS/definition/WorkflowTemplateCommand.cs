namespace Whycespace.Engines.T1M.WSS.Definition;

using Whycespace.Systems.Midstream.WSS.Models;
using Whycespace.Systems.Midstream.WSS.Definition;
using Whycespace.Systems.Midstream.WSS.Execution;
using Whycespace.Systems.Midstream.WSS.Policies;

public sealed record WorkflowTemplateCommand(
    string TemplateName,
    string TemplateDescription,
    int TemplateVersion,
    IReadOnlyList<WorkflowTemplateCommandStep> TemplateSteps,
    IReadOnlyList<WorkflowTemplateParameter> TemplateParameters,
    string RequestedBy,
    DateTimeOffset Timestamp
);

public sealed record WorkflowTemplateCommandStep(
    string StepId,
    string StepName,
    string EngineName,
    IReadOnlyList<string> Dependencies,
    IReadOnlyDictionary<string, string> ParameterBindings,
    TimeSpan Timeout,
    WorkflowFailurePolicy? RetryPolicy
);
