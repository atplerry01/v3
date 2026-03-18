using Whycespace.Engines.T3I.Projections.Models;
using Whycespace.Engines.T3I.Projections.Stores;
using Whycespace.EventFabric.Models;
using Whycespace.ProjectionRuntime.Projections.Contracts;

namespace Whycespace.Engines.T3I.Projections.Implementations;

public sealed class WorkforcePerformanceProjection : IProjection
{
    private readonly AtlasProjectionStore<WorkforcePerformanceModel> _store;

    public WorkforcePerformanceProjection(AtlasProjectionStore<WorkforcePerformanceModel> store)
    {
        _store = store;
    }

    public string Name => "AtlasWorkforcePerformance";

    public IReadOnlyCollection<string> EventTypes =>
    [
        "whyce.heos.task-assigned",
        "whyce.heos.task-completed",
        "whyce.heos.worker-status-changed"
    ];

    public Task HandleAsync(EventEnvelope envelope)
    {
        if (_store.HasProcessed(envelope.EventId))
            return Task.CompletedTask;

        switch (envelope.EventType)
        {
            case "whyce.heos.task-assigned":
                ApplyTaskAssigned(envelope);
                break;

            case "whyce.heos.task-completed":
                ApplyTaskCompleted(envelope);
                break;

            case "whyce.heos.worker-status-changed":
                ApplyStatusChanged(envelope);
                break;
        }

        _store.MarkProcessed(envelope.EventId);
        return Task.CompletedTask;
    }

    public AtlasProjectionStore<WorkforcePerformanceModel> Store => _store;

    private void ApplyTaskAssigned(EventEnvelope envelope)
    {
        if (envelope.Payload is not IDictionary<string, object> data)
            return;

        var workerId = ExtractGuid(data, "WorkerId");
        if (workerId == Guid.Empty) return;

        var current = _store.Get(workerId) ?? NewPerformance(workerId);
        _store.Upsert(workerId, current with
        {
            TasksAssigned = current.TasksAssigned + 1,
            CompletionRate = ComputeRate(current.TasksCompleted, current.TasksAssigned + 1),
            LastUpdatedAt = envelope.Timestamp.Value
        });
    }

    private void ApplyTaskCompleted(EventEnvelope envelope)
    {
        if (envelope.Payload is not IDictionary<string, object> data)
            return;

        var workerId = ExtractGuid(data, "WorkerId");
        if (workerId == Guid.Empty) return;

        var current = _store.Get(workerId) ?? NewPerformance(workerId);
        _store.Upsert(workerId, current with
        {
            TasksCompleted = current.TasksCompleted + 1,
            CompletionRate = ComputeRate(current.TasksCompleted + 1, current.TasksAssigned),
            LastUpdatedAt = envelope.Timestamp.Value
        });
    }

    private void ApplyStatusChanged(EventEnvelope envelope)
    {
        if (envelope.Payload is not IDictionary<string, object> data)
            return;

        var workerId = ExtractGuid(data, "WorkerId");
        if (workerId == Guid.Empty) return;

        data.TryGetValue("Status", out var statusObj);
        var status = statusObj?.ToString() ?? "Unknown";

        var current = _store.Get(workerId) ?? NewPerformance(workerId);
        _store.Upsert(workerId, current with
        {
            CurrentStatus = status,
            LastUpdatedAt = envelope.Timestamp.Value
        });
    }

    private static double ComputeRate(int completed, int assigned) =>
        assigned > 0 ? (double)completed / assigned : 0.0;

    private static WorkforcePerformanceModel NewPerformance(Guid workerId) =>
        new(workerId, 0, 0, 0.0, "Active", DateTimeOffset.UtcNow);

    private static Guid ExtractGuid(IDictionary<string, object> data, string key)
    {
        if (data.TryGetValue(key, out var value) && value is Guid guid)
            return guid;

        if (value is string s && Guid.TryParse(s, out var parsed))
            return parsed;

        return Guid.Empty;
    }
}
