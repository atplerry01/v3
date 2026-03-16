namespace Whycespace.Systems.Midstream.WSS.Stores;

using global::System.Collections.Concurrent;
using Whycespace.Domain.Core.Workflows;

public sealed class WorkflowStateStore : IWorkflowStateStore
{
    private readonly ConcurrentDictionary<string, WorkflowStateRecord> _states = new();
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, WorkflowStepState>> _stepStates = new();

    public void PersistWorkflowState(WorkflowStateRecord record)
    {
        _states[record.InstanceId] = record;

        var steps = new ConcurrentDictionary<string, WorkflowStepState>();
        foreach (var step in record.CompletedSteps)
            steps[step.StepId] = step;
        foreach (var step in record.FailedSteps)
            steps[step.StepId] = step;

        _stepStates[record.InstanceId] = steps;
    }

    public void UpdateStepState(string instanceId, string stepId, StepStatus status)
    {
        if (!_states.TryGetValue(instanceId, out var record))
            throw new KeyNotFoundException($"Workflow state not found: '{instanceId}'");

        if (!_stepStates.TryGetValue(instanceId, out var steps))
            throw new KeyNotFoundException($"Step states not found for workflow: '{instanceId}'");

        var now = DateTimeOffset.UtcNow;

        if (steps.TryGetValue(stepId, out var existing))
        {
            var retryCount = status == StepStatus.Retrying ? existing.RetryCount + 1 : existing.RetryCount;
            var completedAt = status == StepStatus.Completed ? now : existing.CompletedAt;
            var failureTimestamp = status == StepStatus.Failed || status == StepStatus.TimedOut
                ? now
                : existing.LastFailureTimestamp;

            var updated = existing with
            {
                Status = status,
                RetryCount = retryCount,
                CompletedAt = completedAt,
                LastFailureTimestamp = failureTimestamp
            };

            steps[stepId] = updated;
        }
        else
        {
            var newStep = new WorkflowStepState(
                stepId,
                stepId,
                status,
                0,
                now,
                status == StepStatus.Completed ? now : null,
                null
            );

            steps[stepId] = newStep;
        }

        RebuildRecord(instanceId, record, steps);
    }

    public WorkflowStateRecord? GetWorkflowState(string instanceId)
    {
        return _states.TryGetValue(instanceId, out var record) ? record : null;
    }

    public IReadOnlyList<WorkflowStepState> ListPendingSteps(string instanceId)
    {
        if (!_stepStates.TryGetValue(instanceId, out var steps))
            return [];

        return steps.Values
            .Where(s => s.Status == StepStatus.Pending)
            .ToList();
    }

    public IReadOnlyList<WorkflowStepState> ListCompletedSteps(string instanceId)
    {
        if (!_stepStates.TryGetValue(instanceId, out var steps))
            return [];

        return steps.Values
            .Where(s => s.Status == StepStatus.Completed)
            .ToList();
    }

    private void RebuildRecord(string instanceId, WorkflowStateRecord record, ConcurrentDictionary<string, WorkflowStepState> steps)
    {
        var allSteps = steps.Values.ToList();
        var completedSteps = allSteps.Where(s => s.Status == StepStatus.Completed).ToList();
        var failedSteps = allSteps.Where(s => s.Status == StepStatus.Failed || s.Status == StepStatus.TimedOut).ToList();
        var totalRetries = allSteps.Sum(s => s.RetryCount);
        var hasTimeout = allSteps.Any(s => s.Status == StepStatus.TimedOut);

        var updated = record with
        {
            CompletedSteps = completedSteps,
            FailedSteps = failedSteps,
            RetryCount = totalRetries,
            TimeoutStatus = hasTimeout,
            LastUpdated = DateTimeOffset.UtcNow
        };

        _states[instanceId] = updated;
    }
}
