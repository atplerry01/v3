namespace Whycespace.System.Midstream.WSS.Stores;

using global::System.Collections.Concurrent;
using Whycespace.System.Midstream.WSS.Models;

public sealed class WorkflowRegistryStore
{
    private readonly ConcurrentDictionary<string, WorkflowRegistryEntry> _entries = new();

    public void Register(WorkflowRegistryEntry entry)
    {
        if (!_entries.TryAdd(entry.WorkflowId, entry))
            throw new InvalidOperationException($"Workflow already registered: {entry.WorkflowId}");
    }

    public WorkflowRegistryEntry Get(string workflowId)
    {
        if (!_entries.TryGetValue(workflowId, out var entry))
            throw new KeyNotFoundException($"Workflow not registered: {workflowId}");
        return entry;
    }

    public IReadOnlyCollection<WorkflowRegistryEntry> GetAll()
    {
        return _entries.Values.ToList();
    }
}
