namespace Whycespace.Runtime.Persistence.Workflow;

using global::System.Collections.Concurrent;

public sealed class WorkflowRetryStore : IWorkflowRetryStore,
    Whycespace.Engines.T1M.Shared.IWorkflowRetryStore
{
    private readonly ConcurrentDictionary<(string InstanceId, string StepId), int> _retryCounts = new();

    public int GetRetryCount(string instanceId, string stepId)
    {
        return _retryCounts.GetValueOrDefault((instanceId, stepId), 0);
    }

    public int IncrementRetryCount(string instanceId, string stepId)
    {
        return _retryCounts.AddOrUpdate((instanceId, stepId), 1, (_, count) => count + 1);
    }

    public void ResetRetryCount(string instanceId, string stepId)
    {
        _retryCounts.TryRemove((instanceId, stepId), out _);
    }
}
