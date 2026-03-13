namespace Whycespace.WorkflowRuntime.Runtime;

using Whycespace.Contracts.Runtime;
using Whycespace.WorkflowRuntime.Executor;
using Whycespace.WorkflowRuntime.Registry;

public sealed class WorkflowRuntime
{
    private readonly IWorkflowRegistry _registry;
    private readonly IWorkflowExecutor _executor;

    public WorkflowRuntime(IWorkflowRegistry registry, IWorkflowExecutor executor)
    {
        _registry = registry;
        _executor = executor;
    }

    public async Task<ExecutionResult> ExecuteAsync(WorkflowExecutionRequest request)
    {
        var graph = _registry.Resolve(request.WorkflowName)
            ?? throw new InvalidOperationException($"Workflow not found: {request.WorkflowName}");

        return await _executor.ExecuteAsync(graph, request.Context, request.PartitionKey);
    }
}
