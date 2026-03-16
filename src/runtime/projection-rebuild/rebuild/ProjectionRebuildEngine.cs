using Whycespace.EventFabric.Models;
using Whycespace.ProjectionRuntime.Projections.Contracts;
using Whycespace.ProjectionRuntime.Projections.Registry;
using Whycespace.ProjectionRebuild.Checkpoints;
using Whycespace.ProjectionRebuild.Models;
using Whycespace.ProjectionRebuild.Reader;
using Whycespace.ProjectionRebuild.Reset;

namespace Whycespace.ProjectionRebuild.Rebuild;

public sealed class ProjectionRebuildEngine
{
    private readonly EventLogReader _reader;
    private readonly IProjectionRegistry _registry;
    private readonly ProjectionResetService _resetService;
    private readonly ProjectionCheckpointStore _checkpointStore;
    private readonly RebuildStatus _status = new();

    public ProjectionRebuildEngine(
        EventLogReader reader,
        IProjectionRegistry registry,
        ProjectionResetService resetService,
        ProjectionCheckpointStore checkpointStore)
    {
        _reader = reader;
        _registry = registry;
        _resetService = resetService;
        _checkpointStore = checkpointStore;
    }

    public RebuildStatus Status => _status;

    public async Task RebuildAsync(CancellationToken cancellationToken = default)
    {
        _status.Rebuilding = true;
        _status.ProcessedEvents = 0;
        _status.StartedAt = DateTime.UtcNow;
        _status.CompletedAt = null;

        await foreach (var envelope in _reader.ReadAllAsync(cancellationToken))
        {
            await ProcessAsync(envelope);
            _status.ProcessedEvents++;
        }

        _status.Rebuilding = false;
        _status.CompletedAt = DateTime.UtcNow;
    }

    public async Task RebuildProjectionAsync(
        string projectionName,
        CancellationToken cancellationToken = default)
    {
        _status.Rebuilding = true;
        _status.CurrentProjection = projectionName;
        _status.ProcessedEvents = 0;
        _status.StartedAt = DateTime.UtcNow;
        _status.CompletedAt = null;

        await _resetService.ResetAsync(projectionName);

        var checkpoint = await _checkpointStore.LoadCheckpointAsync(projectionName);

        IAsyncEnumerable<EventEnvelope> events = checkpoint is not null
            ? _reader.ReadFromAsync(checkpoint.LastProcessedEventId, cancellationToken)
            : _reader.ReadAllAsync(cancellationToken);

        Guid lastEventId = default;

        await foreach (var envelope in events)
        {
            await ProcessAsync(envelope);
            lastEventId = envelope.EventId;
            _status.ProcessedEvents++;
        }

        if (lastEventId != default)
        {
            await _checkpointStore.SaveCheckpointAsync(new ProjectionCheckpoint(
                projectionName,
                lastEventId,
                DateTime.UtcNow));
        }

        _status.CurrentProjection = null;
        _status.Rebuilding = false;
        _status.CompletedAt = DateTime.UtcNow;
    }

    private async Task ProcessAsync(EventEnvelope envelope)
    {
        var projections = _registry.Resolve(envelope.EventType);

        foreach (var projection in projections)
        {
            await projection.HandleAsync(envelope);
        }
    }
}
