namespace Whycespace.CommandSystem.Routing;

public sealed class CommandRouteRegistry
{
    private readonly Dictionary<string, CommandRouteDescriptor> _byCommandId;
    private readonly IReadOnlyList<CommandRouteDescriptor> _all;

    internal CommandRouteRegistry(IReadOnlyList<CommandRouteDescriptor> routes)
    {
        _all = routes;
        _byCommandId = routes.ToDictionary(r => r.CommandId);
    }

    public string ResolveEngine(string commandId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(commandId);

        if (_byCommandId.TryGetValue(commandId, out var descriptor))
            return descriptor.EngineId;

        throw new CommandRoutingException(
            $"No route registered for command ID '{commandId}'.",
            commandId);
    }

    public bool HasRoute(string commandId) =>
        _byCommandId.ContainsKey(commandId);

    public CommandRouteDescriptor GetRoute(string commandId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(commandId);

        if (_byCommandId.TryGetValue(commandId, out var descriptor))
            return descriptor;

        throw new CommandRoutingException(
            $"No route registered for command ID '{commandId}'.",
            commandId);
    }

    public IReadOnlyList<CommandRouteDescriptor> GetAll() => _all;

    public int Count => _all.Count;
}
