namespace Whycespace.CommandSystem.Routing;

using System.Reflection;

public static class CommandRouteDiscovery
{
    public static IReadOnlyList<CommandRouteDescriptor> DiscoverFromAssembly(Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);
        return DiscoverFromAssemblies([assembly]);
    }

    public static IReadOnlyList<CommandRouteDescriptor> DiscoverFromAssemblies(
        IEnumerable<Assembly> assemblies)
    {
        ArgumentNullException.ThrowIfNull(assemblies);

        var routes = new List<CommandRouteDescriptor>();

        foreach (var assembly in assemblies)
        {
            Type[] types;
            try
            {
                types = assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                types = ex.Types.Where(t => t is not null).ToArray()!;
            }

            foreach (var type in types)
            {
                var attribute = type.GetCustomAttribute<CommandRouteAttribute>();
                if (attribute is null)
                    continue;

                routes.Add(new CommandRouteDescriptor(
                    CommandId: attribute.CommandId,
                    EngineId: attribute.EngineId,
                    CommandType: type
                ));
            }
        }

        return routes.AsReadOnly();
    }
}
