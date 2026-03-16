using System.Collections.Concurrent;
using Whycespace.ProjectionRuntime.Projections.Contracts;

namespace Whycespace.ProjectionRuntime.Projections.Registry;

public sealed class ProjectionRegistry : IProjectionRegistry
{
    private readonly ConcurrentDictionary<string, List<IProjection>> _map = new();
    private readonly List<IProjection> _all = new();
    private readonly object _lock = new();

    public void Register(IProjection projection)
    {
        lock (_lock)
        {
            _all.Add(projection);

            foreach (var eventType in projection.EventTypes)
            {
                var list = _map.GetOrAdd(eventType, _ => new List<IProjection>());
                list.Add(projection);
            }
        }
    }

    public IReadOnlyCollection<IProjection> Resolve(string eventType)
    {
        return _map.TryGetValue(eventType, out var projections)
            ? projections.AsReadOnly()
            : Array.Empty<IProjection>();
    }

    public IReadOnlyCollection<IProjection> GetAll()
    {
        lock (_lock)
        {
            return _all.AsReadOnly();
        }
    }
}
