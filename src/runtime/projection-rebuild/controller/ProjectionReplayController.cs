using Whycespace.Projections.Registry;
using Whycespace.ProjectionRebuild.Models;
using Whycespace.ProjectionRebuild.Rebuild;
using Whycespace.ProjectionRebuild.Reset;

namespace Whycespace.ProjectionRebuild.Controller;

public sealed class ProjectionReplayController
{
    private readonly ProjectionRebuildEngine _rebuildEngine;
    private readonly ProjectionResetService _resetService;
    private readonly IProjectionRegistry _registry;

    public ProjectionReplayController(
        ProjectionRebuildEngine rebuildEngine,
        ProjectionResetService resetService,
        IProjectionRegistry registry)
    {
        _rebuildEngine = rebuildEngine;
        _resetService = resetService;
        _registry = registry;
    }

    public async Task RebuildAllAsync(CancellationToken cancellationToken = default)
    {
        await _resetService.ResetAllAsync();
        await _rebuildEngine.RebuildAsync(cancellationToken);
    }

    public async Task RebuildProjectionAsync(
        string projectionName,
        CancellationToken cancellationToken = default)
    {
        await _rebuildEngine.RebuildProjectionAsync(projectionName, cancellationToken);
    }

    public RebuildStatus GetStatus() => _rebuildEngine.Status;
}
