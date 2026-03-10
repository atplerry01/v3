using System.Collections.Concurrent;
using Whycespace.ProjectionRebuild.Models;

namespace Whycespace.ProjectionRebuild.Checkpoints;

public sealed class ProjectionCheckpointStore
{
    private readonly ConcurrentDictionary<string, ProjectionCheckpoint> _checkpoints = new();

    public Task SaveCheckpointAsync(ProjectionCheckpoint checkpoint)
    {
        _checkpoints[checkpoint.ProjectionName] = checkpoint;
        return Task.CompletedTask;
    }

    public Task<ProjectionCheckpoint?> LoadCheckpointAsync(string projectionName)
    {
        _checkpoints.TryGetValue(projectionName, out var checkpoint);
        return Task.FromResult(checkpoint);
    }

    public Task ClearCheckpointAsync(string projectionName)
    {
        _checkpoints.TryRemove(projectionName, out _);
        return Task.CompletedTask;
    }

    public IReadOnlyCollection<ProjectionCheckpoint> GetAll()
    {
        return _checkpoints.Values.ToList().AsReadOnly();
    }
}
