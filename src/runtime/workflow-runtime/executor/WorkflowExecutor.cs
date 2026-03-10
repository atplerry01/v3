namespace Whycespace.WorkflowRuntime.Executor;

using Whycespace.Contracts.Engines;
using Whycespace.Contracts.Primitives;
using Whycespace.Contracts.Runtime;
using Whycespace.Contracts.Workflows;
using Whycespace.WorkflowRuntime.Context;
using Whycespace.WorkflowRuntime.Events;

public sealed class WorkflowExecutor : IWorkflowExecutor
{
    private readonly Func<WorkflowStep, string, PartitionKey, IReadOnlyDictionary<string, object>, Task<EngineResult>> _executeStep;

    public WorkflowExecutor(
        Func<WorkflowStep, string, PartitionKey, IReadOnlyDictionary<string, object>, Task<EngineResult>> executeStep)
    {
        _executeStep = executeStep;
    }

    public async Task<ExecutionResult> ExecuteAsync(
        WorkflowGraph graph,
        IReadOnlyDictionary<string, object> input,
        PartitionKey partitionKey = default)
    {
        var effectivePartitionKey = partitionKey.IsEmpty
            ? new PartitionKey(graph.WorkflowId)
            : partitionKey;

        var instance = new WorkflowInstance(
            workflowInstanceId: Guid.NewGuid(),
            workflowName: graph.Name,
            partitionKey: effectivePartitionKey,
            input: input);

        var context = new Dictionary<string, object>(input);
        var allEvents = new List<EngineEvent>();

        WorkflowStartedEvent.Create(graph.Name, instance.WorkflowInstanceId.ToString());

        for (var i = 0; i < graph.Steps.Count; i++)
        {
            instance.CurrentStepIndex = i;
            var step = graph.Steps[i];

            var result = await _executeStep(
                step, graph.WorkflowId, instance.PartitionKey, context);

            allEvents.AddRange(result.Events);

            if (!result.Success)
            {
                WorkflowCompletedEvent.Create(graph.Name, instance.WorkflowInstanceId.ToString(), false);
                var errorMsg = result.Output.TryGetValue("error", out var err)
                    ? err?.ToString() ?? $"Step '{step.Name}' failed"
                    : $"Step '{step.Name}' failed";
                return ExecutionResult.Fail(errorMsg);
            }

            foreach (var kvp in result.Output)
                context[kvp.Key] = kvp.Value;
        }

        WorkflowCompletedEvent.Create(graph.Name, instance.WorkflowInstanceId.ToString(), true);

        return ExecutionResult.Ok(context);
    }
}
