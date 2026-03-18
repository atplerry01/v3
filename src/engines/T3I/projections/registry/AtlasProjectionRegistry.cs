using Whycespace.ProjectionRuntime.Projections.Contracts;

namespace Whycespace.Engines.T3I.Projections.Registry;

public sealed class AtlasProjectionRegistry
{
    private readonly Dictionary<string, IProjection> _projections = new(StringComparer.Ordinal);

    public void Register(IProjection projection)
    {
        _projections[projection.Name] = projection;
    }

    public IProjection? Get(string name) =>
        _projections.GetValueOrDefault(name);

    public IReadOnlyCollection<IProjection> GetAll() => _projections.Values.ToList();

    public IReadOnlyCollection<string> GetRegisteredNames() => _projections.Keys.ToList();

    public IReadOnlyCollection<string> GetAllEventTypes() =>
        _projections.Values
            .SelectMany(p => p.EventTypes)
            .Distinct(StringComparer.Ordinal)
            .ToList();

    public IReadOnlyCollection<IProjection> GetProjectionsForEventType(string eventType) =>
        _projections.Values
            .Where(p => p.EventTypes.Contains(eventType))
            .ToList();

    public int Count => _projections.Count;
}
