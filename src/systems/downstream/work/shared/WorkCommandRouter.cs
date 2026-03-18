namespace Whycespace.Systems.Downstream.Work.Shared;

public sealed class WorkCommandRouter
{
    private readonly Dictionary<string, Func<object, Task>> _routes = new();

    public void RegisterRoute(string commandType, Func<object, Task> handler)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(commandType);
        ArgumentNullException.ThrowIfNull(handler);

        _routes[commandType] = handler;
    }

    public async Task RouteAsync(string commandType, object command)
    {
        if (!_routes.TryGetValue(commandType, out var handler))
            throw new InvalidOperationException($"No route registered for command type '{commandType}'.");

        await handler(command);
    }

    public bool HasRoute(string commandType) => _routes.ContainsKey(commandType);
}
