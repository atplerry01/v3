namespace Whycespace.System.Midstream.WSS.Models;

public sealed record WorkflowStepDefinition(
    string StepId,
    string Name,
    string EngineName,
    string Description,
    IReadOnlyList<string> NextSteps,
    WorkflowFailurePolicy? FailurePolicy
);
