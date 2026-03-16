using Whycespace.ProjectionRuntime.Projections.Contracts;
using Whycespace.ProjectionRuntime.Projections.Registry;
using Whycespace.ProjectionRuntime.Storage;

namespace Whycespace.ProjectionRebuild.Reset;

public sealed class ProjectionResetService
{
    private readonly IProjectionStore _store;
    private readonly IProjectionRegistry _registry;

    public ProjectionResetService(IProjectionStore store, IProjectionRegistry registry)
    {
        _store = store;
        _registry = registry;
    }

    public async Task ResetAsync(string projectionName)
    {
        await _store.DeleteAsync($"projection:{projectionName}");
    }

    public async Task ResetAllAsync()
    {
        foreach (var projection in _registry.GetAll())
        {
            await _store.DeleteAsync($"projection:{projection.Name}");
        }
    }
}
