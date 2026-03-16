namespace Whycespace.Systems.Midstream.WSS.Stores;

using global::System.Collections.Concurrent;
using Whycespace.Systems.Midstream.WSS.Instances;
using Whycespace.Systems.Midstream.WSS.Models;

public sealed class WorkflowInstanceStore : IWorkflowInstanceStore
{
    private readonly ConcurrentDictionary<string, WorkflowInstanceRecord> _byId = new();
    private readonly ConcurrentDictionary<string, string> _byCorrelationId = new();
    private readonly ConcurrentDictionary<string, List<string>> _byWorkflowName = new();

    public void Insert(WorkflowInstanceRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);

        if (!_byId.TryAdd(record.InstanceId, record))
            throw new InvalidOperationException($"Workflow instance already exists: {record.InstanceId}");

        if (!string.IsNullOrWhiteSpace(record.CorrelationId))
            _byCorrelationId[record.CorrelationId] = record.InstanceId;

        _byWorkflowName.AddOrUpdate(
            record.WorkflowName,
            _ => new List<string> { record.InstanceId },
            (_, list) => { list.Add(record.InstanceId); return list; });
    }

    public void UpdateStatus(string instanceId, WorkflowInstanceRecord record)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(instanceId);
        ArgumentNullException.ThrowIfNull(record);

        if (!_byId.ContainsKey(instanceId))
            throw new KeyNotFoundException($"Workflow instance not found: {instanceId}");

        _byId[instanceId] = record;
    }

    public WorkflowInstanceRecord? GetById(string instanceId)
    {
        return _byId.TryGetValue(instanceId, out var record) ? record : null;
    }

    public IReadOnlyList<WorkflowInstanceRecord> GetByWorkflowName(string workflowName)
    {
        if (!_byWorkflowName.TryGetValue(workflowName, out var instanceIds))
            return Array.Empty<WorkflowInstanceRecord>();

        var results = new List<WorkflowInstanceRecord>();
        foreach (var id in instanceIds)
        {
            if (_byId.TryGetValue(id, out var record))
                results.Add(record);
        }

        return results;
    }

    public WorkflowInstanceRecord? GetByCorrelationId(string correlationId)
    {
        if (!_byCorrelationId.TryGetValue(correlationId, out var instanceId))
            return null;

        return _byId.TryGetValue(instanceId, out var record) ? record : null;
    }

    public IReadOnlyList<WorkflowInstanceRecord> GetActive()
    {
        return _byId.Values
            .Where(r => r.Status is WorkflowInstanceStatus.Created
                or WorkflowInstanceStatus.Running
                or WorkflowInstanceStatus.Waiting
                or WorkflowInstanceStatus.Retrying)
            .ToList();
    }
}
