namespace Whycespace.WorkflowRuntime.Executor;

using Whycespace.Shared.Primitives.Common;
using Whycespace.Contracts.Runtime;
using Whycespace.Contracts.Workflows;

public interface IWorkflowExecutor
{
    Task<ExecutionResult> ExecuteAsync(
        WorkflowGraph graph,
        IReadOnlyDictionary<string, object> input,
        PartitionKey partitionKey = default);
}
