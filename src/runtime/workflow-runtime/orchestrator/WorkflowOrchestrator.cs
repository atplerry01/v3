namespace Whycespace.WorkflowRuntime;

using Whycespace.Contracts.Engines;
using Whycespace.Contracts.Runtime;
using Whycespace.Contracts.Workflows;

public sealed class WorkflowOrchestrator : IWorkflowOrchestrator
{
    private readonly IEngineRuntimeDispatcher _dispatcher;
    private readonly WorkflowStateStore _stateStore;

    public WorkflowOrchestrator(IEngineRuntimeDispatcher dispatcher, WorkflowStateStore stateStore)
    {
        _dispatcher = dispatcher;
        _stateStore = stateStore;
    }

    public async Task<WorkflowState> ExecuteWorkflowAsync(
        WorkflowGraph graph,
        IReadOnlyDictionary<string, object> initialContext)
    {
        var workflowId = graph.WorkflowId;
        var context = new Dictionary<string, object>(initialContext);
        var state = new WorkflowState(
            workflowId, graph.Steps[0].StepId, WorkflowStatus.Running,
            context, DateTimeOffset.UtcNow, null);

        _stateStore.Save(state);

        foreach (var step in graph.Steps)
        {
            state = state with { CurrentStepId = step.StepId };
            _stateStore.Save(state);

            var envelope = new EngineInvocationEnvelope(
                Guid.NewGuid(), step.EngineName, workflowId,
                step.StepId, workflowId, context);

            var result = await _dispatcher.DispatchAsync(envelope);

            if (!result.Success)
            {
                state = state with { Status = WorkflowStatus.Failed, CompletedAt = DateTimeOffset.UtcNow };
                _stateStore.Save(state);
                return state;
            }

            foreach (var kvp in result.Output)
                context[kvp.Key] = kvp.Value;
        }

        state = state with { Status = WorkflowStatus.Completed, CompletedAt = DateTimeOffset.UtcNow, Context = context };
        _stateStore.Save(state);
        return state;
    }
}
