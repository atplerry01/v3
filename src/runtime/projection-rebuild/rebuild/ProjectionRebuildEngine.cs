using Whycespace.Projections.Engine;
using Whycespace.ProjectionRebuild.Checkpoints;
using Whycespace.ProjectionRebuild.Models;
using Whycespace.ProjectionRebuild.Reader;
using Whycespace.ProjectionRebuild.Reset;

namespace Whycespace.ProjectionRebuild.Rebuild;

public sealed class ProjectionRebuildEngine
{
    private readonly EventLogReader _reader;
    private readonly ProjectionEngine _projectionEngine;
    private readonly ProjectionResetService _resetService;
    private readonly ProjectionCheckpointStore _checkpointStore;
    private readonly RebuildStatus _status = new();

    public ProjectionRebuildEngine(
        EventLogReader reader,
        ProjectionEngine projectionEngine,
        ProjectionResetService resetService,
        ProjectionCheckpointStore checkpointStore)
    {
        _reader = reader;
        _projectionEngine = projectionEngine;
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
            await _projectionEngine.ProcessAsync(envelope);
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

        IAsyncEnumerable<EventFabric.Models.EventEnvelope> events = checkpoint is not null
            ? _reader.ReadFromAsync(checkpoint.LastProcessedEventId, cancellationToken)
            : _reader.ReadAllAsync(cancellationToken);

        Guid lastEventId = default;

        await foreach (var envelope in events)
        {
            await _projectionEngine.ProcessAsync(envelope);
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
}
