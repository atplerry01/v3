namespace Whycespace.Platform.RuntimeClient;

using Whycespace.Contracts.Engines;
using Whycespace.Contracts.Runtime;
using Whycespace.Contracts.Workflows;

public sealed class WorkflowClient : IWorkflowOrchestrator
{
    private readonly IWorkflowOrchestrator _orchestrator;

    public WorkflowClient(IWorkflowOrchestrator orchestrator)
    {
        _orchestrator = orchestrator;
    }

    public Task<WorkflowState> ExecuteWorkflowAsync(
        WorkflowGraph graph,
        IReadOnlyDictionary<string, object> initialContext)
    {
        return _orchestrator.ExecuteWorkflowAsync(graph, initialContext);
    }
}
