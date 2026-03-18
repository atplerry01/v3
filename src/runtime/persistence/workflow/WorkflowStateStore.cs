namespace Whycespace.Infrastructure.Persistence.Workflow;

using global::System.Collections.Concurrent;
using Whycespace.Contracts.Workflows;

public sealed class WorkflowStateStore
{
    private readonly ConcurrentDictionary<Guid, WorkflowRuntimeState> _states = new();

    public void Save(WorkflowRuntimeState state)
    {
        _states[state.InstanceId] = state;
    }

    public WorkflowRuntimeState? Get(Guid instanceId)
    {
        return _states.TryGetValue(instanceId, out var state) ? state : null;
    }

    public WorkflowRuntimeState Update(Guid instanceId, string currentNode, IReadOnlyDictionary<string, object>? contextData = null)
    {
        var existing = Get(instanceId);
        var merged = contextData ?? existing?.ContextData ?? new Dictionary<string, object>();

        var updated = new WorkflowRuntimeState(instanceId, currentNode, merged, DateTimeOffset.UtcNow);
        _states[instanceId] = updated;
        return updated;
    }
}
