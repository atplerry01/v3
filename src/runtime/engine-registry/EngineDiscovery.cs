namespace Whycespace.Runtime.EngineRegistry;

using System.Reflection;
using Whycespace.Contracts.Engines;

public static class EngineDiscovery
{
    public static IReadOnlyList<EngineDescriptor> DiscoverEngines(Assembly assembly)
    {
        return DiscoverEngines(new[] { assembly });
    }

    public static IReadOnlyList<EngineDescriptor> DiscoverEngines(IEnumerable<Assembly> assemblies)
    {
        var descriptors = new List<EngineDescriptor>();

        foreach (var assembly in assemblies)
        {
            var engineTypes = assembly.GetTypes()
                .Where(t => t is { IsClass: true, IsAbstract: false }
                            && typeof(IEngine).IsAssignableFrom(t)
                            && t.GetCustomAttribute<EngineManifestAttribute>() is not null);

            foreach (var type in engineTypes)
            {
                EngineRegistryValidator.ValidateEngineType(type);

                var attribute = type.GetCustomAttribute<EngineManifestAttribute>()!;

                descriptors.Add(new EngineDescriptor(
                    EngineId: attribute.EngineId,
                    Tier: attribute.Tier,
                    EngineType: type,
                    CommandType: attribute.CommandType,
                    ResultType: attribute.ResultType
                ));
            }
        }

        return descriptors.AsReadOnly();
    }
}
