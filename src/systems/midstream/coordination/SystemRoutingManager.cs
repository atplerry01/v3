namespace Whycespace.Systems.Midstream.Coordination;

public sealed class SystemRoutingManager
{
    private readonly Dictionary<string, string> _routes = new();

    public void RegisterRoute(string systemName, string targetEndpoint)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(systemName);
        ArgumentException.ThrowIfNullOrWhiteSpace(targetEndpoint);
        _routes[systemName] = targetEndpoint;
    }

    public string? ResolveRoute(string systemName)
    {
        return _routes.GetValueOrDefault(systemName);
    }

    public IReadOnlyDictionary<string, string> GetAllRoutes() => _routes.AsReadOnly();
}
