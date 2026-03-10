namespace Whycespace.Platform.RuntimeClient;

using Whycespace.Shared.Contracts;
using Whycespace.Shared.Workflow;

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
