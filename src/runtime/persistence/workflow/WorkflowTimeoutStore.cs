namespace Whycespace.Runtime.Persistence.Workflow;

using global::System.Collections.Concurrent;
using Whycespace.Systems.Midstream.WSS.Models;

public sealed class WorkflowTimeoutStore : IWorkflowTimeoutStore
{
    private readonly ConcurrentDictionary<(string InstanceId, string StepId), TimeoutEntry> _timeouts = new();

    public void RegisterTimeout(string instanceId, string stepId, TimeoutEntry entry)
    {
        _timeouts[(instanceId, stepId)] = entry;
    }

    public TimeoutEntry? GetTimeout(string instanceId, string stepId)
    {
        return _timeouts.GetValueOrDefault((instanceId, stepId));
    }

    public void RemoveTimeout(string instanceId, string stepId)
    {
        _timeouts.TryRemove((instanceId, stepId), out _);
    }

    public IReadOnlyList<TimeoutEntry> ListTimeouts()
    {
        return _timeouts.Values.ToList().AsReadOnly();
    }
}
