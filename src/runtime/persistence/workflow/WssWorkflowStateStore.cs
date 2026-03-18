namespace Whycespace.Runtime.Persistence.Workflow;

using global::System.Collections.Concurrent;
using Whycespace.Runtime.Persistence.Workflow;
using Whycespace.Systems.Midstream.WSS.Models;
using Whycespace.Systems.Midstream.WSS.Definition;
using Whycespace.Systems.Midstream.WSS.Execution;
using Whycespace.Systems.Midstream.WSS.Policies;

public sealed class WssWorkflowStateStore : IWssWorkflowStateStore,
    Whycespace.Engines.T1M.Shared.IWssWorkflowStateStore
{
    private readonly ConcurrentDictionary<string, WorkflowState> _states = new();

    public void SaveState(WorkflowState state)
    {
        if (!_states.TryAdd(state.InstanceId, state))
            throw new InvalidOperationException($"Workflow state already exists: '{state.InstanceId}'");
    }

    public WorkflowState GetState(string instanceId)
    {
        if (!_states.TryGetValue(instanceId, out var state))
            throw new KeyNotFoundException($"Workflow state not found: '{instanceId}'");
        return state;
    }

    public WorkflowState UpdateState(string instanceId, string currentStep, WorkflowInstanceStatus status)
    {
        if (!_states.TryGetValue(instanceId, out var existing))
            throw new KeyNotFoundException($"Workflow state not found: '{instanceId}'");

        var updated = existing with
        {
            CurrentStep = currentStep,
            Status = status,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        _states[instanceId] = updated;
        return updated;
    }

    public WorkflowState AddCompletedStep(string instanceId, string stepId)
    {
        if (!_states.TryGetValue(instanceId, out var existing))
            throw new KeyNotFoundException($"Workflow state not found: '{instanceId}'");

        if (existing.CompletedSteps.Contains(stepId))
            return existing;

        var completedSteps = existing.CompletedSteps.ToList();
        completedSteps.Add(stepId);

        var updated = existing with
        {
            CompletedSteps = completedSteps,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        _states[instanceId] = updated;
        return updated;
    }

    public void DeleteState(string instanceId)
    {
        if (!_states.TryRemove(instanceId, out _))
            throw new KeyNotFoundException($"Workflow state not found: '{instanceId}'");
    }

    public IReadOnlyList<WorkflowState> ListActiveStates()
    {
        return _states.Values.Where(s => s.Status == WorkflowInstanceStatus.Running).ToList();
    }
}
