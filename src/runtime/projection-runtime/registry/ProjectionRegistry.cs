namespace Whycespace.ProjectionRuntime.Registry;

public sealed class ProjectionRegistry
{
    private readonly Dictionary<string, string> _projections = new();

    public void Register(string eventType, string projectionName)
    {
        _projections[eventType] = projectionName;
    }

    public string Resolve(string eventType)
    {
        if (!_projections.TryGetValue(eventType, out var projection))
            throw new InvalidOperationException("Projection not registered");

        return projection;
    }

    public IReadOnlyDictionary<string, string> GetMappings()
    {
        return _projections;
    }
}
