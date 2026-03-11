namespace Whycespace.System.Midstream.WSS.Stores;

using global::System.Collections.Concurrent;
using Whycespace.System.Midstream.WSS.Models;

public sealed class WorkflowVersionStore
{
    private readonly ConcurrentDictionary<string, List<WorkflowVersion>> _store = new();

    public void Store(WorkflowVersion version)
    {
        _store.AddOrUpdate(
            version.WorkflowId,
            _ => new List<WorkflowVersion> { version },
            (_, list) => { list.Add(version); return list; });
    }

    public IReadOnlyList<WorkflowVersion> GetVersions(string workflowId)
    {
        if (!_store.TryGetValue(workflowId, out var versions))
            return Array.Empty<WorkflowVersion>();

        return versions.OrderBy(v => v.Version).ToList();
    }

    public WorkflowVersion? GetActive(string workflowId)
    {
        if (!_store.TryGetValue(workflowId, out var versions) || versions.Count == 0)
            return null;

        return versions.FirstOrDefault(v => v.Status == WorkflowVersionStatus.Active);
    }

    public bool VersionExists(string workflowId, int version)
    {
        if (!_store.TryGetValue(workflowId, out var versions))
            return false;

        return versions.Any(v => v.Version == version);
    }

    public void Update(WorkflowVersion updated)
    {
        if (!_store.TryGetValue(updated.WorkflowId, out var versions))
            throw new KeyNotFoundException($"No versions found for workflow: {updated.WorkflowId}");

        var index = versions.FindIndex(v => v.Version == updated.Version);
        if (index < 0)
            throw new KeyNotFoundException($"Version {updated.Version} not found for workflow: {updated.WorkflowId}");

        versions[index] = updated;
    }
}
