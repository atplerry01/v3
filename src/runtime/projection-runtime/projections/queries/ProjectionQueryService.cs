using Whycespace.ProjectionRuntime.Storage;

namespace Whycespace.ProjectionRuntime.Projections.Queries;

public sealed class ProjectionQueryService
{
    private readonly IProjectionStore _store;

    public ProjectionQueryService(IProjectionStore store)
    {
        _store = store;
    }

    public Task<string?> GetAsync(string key)
    {
        return _store.GetAsync(key);
    }
}
