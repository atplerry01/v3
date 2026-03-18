namespace Whycespace.CommandSystem.Routing;

using System.Reflection;
using Whycespace.Runtime.EngineRegistry;

public sealed class CommandRouteRegistryBuilder
{
    private readonly List<Assembly> _assemblies = [];
    private readonly List<CommandRouteDescriptor> _manualRoutes = [];
    private EngineRegistry? _engineRegistry;

    public CommandRouteRegistryBuilder AddAssembly(Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);

        if (!_assemblies.Contains(assembly))
            _assemblies.Add(assembly);

        return this;
    }

    public CommandRouteRegistryBuilder AddRoute(CommandRouteDescriptor route)
    {
        ArgumentNullException.ThrowIfNull(route);
        _manualRoutes.Add(route);
        return this;
    }

    public CommandRouteRegistryBuilder WithEngineRegistry(EngineRegistry engineRegistry)
    {
        _engineRegistry = engineRegistry ?? throw new ArgumentNullException(nameof(engineRegistry));
        return this;
    }

    public CommandRouteRegistry Build()
    {
        var discovered = CommandRouteDiscovery.DiscoverFromAssemblies(_assemblies);
        var allRoutes = discovered.Concat(_manualRoutes).ToList();

        CommandRouteValidator.ValidateNoDuplicates(allRoutes);
        CommandRouteValidator.ValidateNoCircularRoutes(allRoutes);

        if (_engineRegistry is not null)
            CommandRouteValidator.ValidateEngineReferences(allRoutes, _engineRegistry);

        return new CommandRouteRegistry(allRoutes.AsReadOnly());
    }
}
