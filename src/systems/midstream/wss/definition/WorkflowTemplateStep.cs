namespace Whycespace.Systems.Midstream.WSS.Definition;

using Whycespace.Systems.Midstream.WSS.Policies;

public sealed record WorkflowTemplateStep(
    string StepId,
    string Description,
    string Engine,
    string Command,
    IReadOnlyDictionary<string, string> Parameters,
    WorkflowFailurePolicy? FailurePolicy
);
