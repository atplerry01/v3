namespace Whycespace.Systems.Midstream.WSS.Definition;

using Whycespace.Systems.Midstream.WSS.Policies;

public sealed record WorkflowStepDefinition(
    string StepId,
    string Name,
    string EngineName,
    string Description,
    IReadOnlyList<string> NextSteps,
    WorkflowFailurePolicy? FailurePolicy
);
