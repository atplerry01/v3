namespace Whycespace.Systems.Midstream.WSS.Models;

public sealed record WorkflowStepDefinition(
    string StepId,
    string Name,
    string EngineName,
    string Description,
    IReadOnlyList<string> NextSteps,
    WorkflowFailurePolicy? FailurePolicy
);
