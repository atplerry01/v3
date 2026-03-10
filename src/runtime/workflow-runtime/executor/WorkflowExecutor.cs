namespace Whycespace.WorkflowRuntime.Executor;

using Whycespace.Contracts.Runtime;
using Whycespace.Contracts.Workflows;
using Whycespace.WorkflowRuntime.Context;
using Whycespace.WorkflowRuntime.Events;
using Whycespace.WorkflowRuntime.Step;

public sealed class WorkflowExecutor : IWorkflowExecutor
{
    private readonly WorkflowStepExecutor _stepExecutor;

    public WorkflowExecutor(WorkflowStepExecutor stepExecutor)
    {
        _stepExecutor = stepExecutor;
    }

    public async Task<ExecutionResult> ExecuteAsync(
        WorkflowGraph graph,
        IReadOnlyDictionary<string, object> input)
    {
        var instance = new WorkflowInstance(
            workflowInstanceId: Guid.NewGuid(),
            workflowName: graph.Name,
            partitionKey: graph.WorkflowId,
            input: input);

        var context = new Dictionary<string, object>(input);
        var allEvents = new List<Contracts.Engines.EngineEvent>();

        WorkflowStartedEvent.Create(graph.Name, instance.WorkflowInstanceId.ToString());

        for (var i = 0; i < graph.Steps.Count; i++)
        {
            instance.CurrentStepIndex = i;
            var step = graph.Steps[i];

            var result = await _stepExecutor.ExecuteStepAsync(
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
