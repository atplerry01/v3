namespace Whycespace.Runtime.EngineMetadata.Discovery;

using System.Reflection;
using Whycespace.Contracts.Engines;
using Whycespace.Runtime.EngineMetadata.Manifest;
using Whycespace.Runtime.EngineMetadata.Models;
using Whycespace.Runtime.EngineMetadata.Validation;

public static class EngineDiscovery
{
    public static IReadOnlyList<EngineRegistryDescriptor> DiscoverEngines(Assembly assembly)
    {
        return DiscoverEngines(new[] { assembly });
    }

    public static IReadOnlyList<EngineRegistryDescriptor> DiscoverEngines(IEnumerable<Assembly> assemblies)
    {
        var descriptors = new List<EngineRegistryDescriptor>();

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

                descriptors.Add(new EngineRegistryDescriptor(
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
