namespace Whycespace.Shared.Contracts;

using Whycespace.Shared.Workflow;

public interface IWorkflowOrchestrator
{
    Task<WorkflowState> ExecuteWorkflowAsync(
        WorkflowGraph graph,
        IReadOnlyDictionary<string, object> initialContext);
}
