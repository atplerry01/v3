namespace Whycespace.Systems.Midstream.WSS.Stores;

using global::System.Collections.Concurrent;
using Whycespace.Systems.Midstream.WSS.Registry;

public sealed class WorkflowRegistryStore : IWorkflowRegistryStore
{
    private readonly ConcurrentDictionary<string, WorkflowRegistryRecord> _byId = new();
    private readonly ConcurrentDictionary<string, List<WorkflowRegistryRecord>> _byName = new();

    public void Save(WorkflowRegistryRecord record)
    {
        if (!_byId.TryAdd(record.WorkflowId, record))
            throw new InvalidOperationException($"Workflow already exists: {record.WorkflowId}");

        _byName.AddOrUpdate(
            record.WorkflowName,
            _ => new List<WorkflowRegistryRecord> { record },
            (_, list) => { list.Add(record); return list; });
    }

    public void Update(WorkflowRegistryRecord record)
    {
        if (!_byId.TryGetValue(record.WorkflowId, out _))
            throw new KeyNotFoundException($"Workflow not found: {record.WorkflowId}");

        _byId[record.WorkflowId] = record;

        if (_byName.TryGetValue(record.WorkflowName, out var list))
        {
            var index = list.FindIndex(r => r.WorkflowId == record.WorkflowId);
            if (index >= 0)
                list[index] = record;
        }
    }

    public WorkflowRegistryRecord? GetById(string workflowId)
    {
        return _byId.TryGetValue(workflowId, out var record) ? record : null;
    }

    public WorkflowRegistryRecord? GetByName(string workflowName)
    {
        if (!_byName.TryGetValue(workflowName, out var records) || records.Count == 0)
            return null;

        return records
            .Where(r => r.Status == WorkflowRegistryRecordStatus.Active)
            .MaxBy(r => r.WorkflowVersion);
    }

    public WorkflowRegistryRecord? GetByNameAndVersion(string workflowName, string version)
    {
        if (!_byName.TryGetValue(workflowName, out var records))
            return null;

        return records.Find(r => r.WorkflowVersion == version);
    }

    public IReadOnlyList<WorkflowRegistryRecord> GetAll()
    {
        return _byId.Values.ToList();
    }

    public IReadOnlyList<WorkflowRegistryRecord> GetByType(WorkflowType type)
    {
        return _byId.Values.Where(r => r.WorkflowType == type).ToList();
    }

    public bool ExistsById(string workflowId)
    {
        return _byId.ContainsKey(workflowId);
    }

    public bool ExistsByNameAndVersion(string workflowName, string version)
    {
        if (!_byName.TryGetValue(workflowName, out var records))
            return false;

        return records.Exists(r => r.WorkflowVersion == version);
    }
}
