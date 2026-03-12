namespace Whycespace.Engines.T1M.WSS.Graph;

using Whycespace.System.Midstream.WSS.Models;

public interface IWorkflowGraphEngine
{
    WorkflowGraph BuildGraph(IEnumerable<WorkflowStepDefinition> steps);

    IReadOnlyList<string> ValidateGraph(WorkflowGraph graph);

    IReadOnlyList<string> GetNextSteps(WorkflowGraph graph, string currentStep);

    IReadOnlyList<string> GetStartSteps(WorkflowGraph graph);
}
