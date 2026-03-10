using Whycespace.Reliability.Models;

namespace Whycespace.Reliability.State;

public sealed class InMemoryWorkflowStateStore : IWorkflowStateStore
{
    private readonly Dictionary<Guid, WorkflowStateEntry> _store = new();

    public Task SaveAsync(WorkflowStateEntry entry)
    {
        entry.LastUpdated = DateTime.UtcNow;
        _store[entry.WorkflowInstanceId] = entry;
        return Task.CompletedTask;
    }

    public Task<WorkflowStateEntry?> LoadAsync(Guid workflowInstanceId)
    {
        _store.TryGetValue(workflowInstanceId, out var entry);
        return Task.FromResult(entry);
    }

    public Task<IReadOnlyCollection<WorkflowStateEntry>> GetActiveWorkflowsAsync()
    {
        IReadOnlyCollection<WorkflowStateEntry> result = _store.Values.ToList();
        return Task.FromResult(result);
    }

    public int Count => _store.Count;
}
