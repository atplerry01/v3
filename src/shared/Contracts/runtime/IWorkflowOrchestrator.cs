namespace Whycespace.Contracts.Runtime;

using Whycespace.Contracts.Workflows;

public interface IWorkflowOrchestrator
{
    Task<WorkflowState> ExecuteWorkflowAsync(
        WorkflowGraph graph,
        IReadOnlyDictionary<string, object> initialContext);
}
