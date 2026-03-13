namespace Whycespace.Engines.T1M.WSS.Dependency;

using Whycespace.System.Midstream.WSS.Models;

public interface IWorkflowDependencyEngine
{
    WorkflowDependencyResult AnalyzeWorkflowDependencies(WorkflowDefinition workflow);

    IReadOnlyList<string> ResolveExecutionOrder(WorkflowDefinition workflow);

    IReadOnlyList<string> DetectCircularDependencies(WorkflowDefinition workflow);

    IReadOnlyList<string> GetExternalWorkflowDependencies(WorkflowDefinition workflow);
}
