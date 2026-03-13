namespace Whycespace.Contracts.Runtime;

public interface IWorkflowRuntime
{
    Task<ExecutionResult> ExecuteAsync(WorkflowExecutionRequest request);
}
