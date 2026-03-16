namespace Whycespace.Systems.Midstream.WSS.Models;

public sealed record WorkflowTemplateStep(
    string StepId,
    string Description,
    string Engine,
    string Command,
    IReadOnlyDictionary<string, string> Parameters,
    WorkflowFailurePolicy? FailurePolicy
);
