namespace Whycespace.Engines.T1M.WSS.Graph;

public sealed record WorkflowGraphCommand(
    string WorkflowId,
    string WorkflowName,
    string WorkflowVersion,
    IReadOnlyList<WorkflowGraphStepInput> WorkflowSteps
);

public sealed record WorkflowGraphStepInput(
    string StepId,
    string StepName,
    string EngineName,
    IReadOnlyList<string> Dependencies
);
