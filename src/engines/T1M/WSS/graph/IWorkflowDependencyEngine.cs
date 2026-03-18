namespace Whycespace.Engines.T1M.WSS.Graph;

using Whycespace.Systems.Midstream.WSS.Models;
using Whycespace.Systems.Midstream.WSS.Definition;
using Whycespace.Systems.Midstream.WSS.Execution;
using Whycespace.Systems.Midstream.WSS.Policies;

public interface IWorkflowDependencyEngine
{
    WorkflowDependencyResult AnalyzeWorkflowDependencies(WorkflowDefinition workflow);

    IReadOnlyList<string> ResolveExecutionOrder(WorkflowDefinition workflow);

    IReadOnlyList<string> DetectCircularDependencies(WorkflowDefinition workflow);

    IReadOnlyList<string> GetExternalWorkflowDependencies(WorkflowDefinition workflow);
}
