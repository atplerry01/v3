namespace Whycespace.ProjectionRuntime.Engine;

using Whycespace.ProjectionRuntime.Models;
using Whycespace.ProjectionRuntime.Registry;
using Whycespace.ProjectionRuntime.Storage;

public sealed class ProjectionEngine
{
    private readonly ProjectionRegistry _registry;
    private readonly ProjectionStateStore _store;

    public ProjectionEngine(
        ProjectionRegistry registry,
        ProjectionStateStore store)
    {
        _registry = registry;
        _store = store;
    }

    public void Apply(string eventType, string entityId, object state)
    {
        var projection = _registry.Resolve(eventType);

        var record = new ProjectionRecord(projection, entityId, state);

        _store.Save(record);
    }
}
