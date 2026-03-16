namespace Whycespace.Engines.T1M.WSS.Graph;

using Whycespace.Systems.Midstream.WSS.Models;

public interface IWorkflowGraphEngine
{
    WorkflowGraph BuildGraph(IEnumerable<WorkflowStepDefinition> steps);

    IReadOnlyList<string> ValidateGraph(WorkflowGraph graph);

    IReadOnlyList<string> GetNextSteps(WorkflowGraph graph, string currentStep);

    IReadOnlyList<string> GetStartSteps(WorkflowGraph graph);

    IReadOnlyList<string> ComputeExecutionOrder(WorkflowGraph graph);

    IReadOnlyList<IReadOnlyList<string>> ComputeParallelGroups(WorkflowGraph graph);

    WorkflowGraphResult BuildExecutionGraph(WorkflowGraphCommand command);
}
