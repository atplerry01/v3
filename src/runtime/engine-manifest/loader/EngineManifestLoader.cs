namespace Whycespace.EngineManifest.Loader;

using System.Reflection;
using Whycespace.Contracts.Engines;
using Whycespace.EngineManifest.Manifest;
using Whycespace.EngineManifest.Models;
using Whycespace.EngineManifest.Validation;
using Whycespace.EngineRuntime.Registry;

public sealed class EngineManifestLoader
{
    private readonly IEngineRegistry _registry;
    private readonly List<EngineDescriptor> _descriptors = new();

    public EngineManifestLoader(IEngineRegistry registry)
    {
        _registry = registry;
    }

    public void LoadFromAssembly(Assembly assembly)
    {
        var engineTypes = assembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false }
                        && typeof(IEngine).IsAssignableFrom(t)
                        && t.GetCustomAttribute<EngineManifestAttribute>() is not null);

        foreach (var type in engineTypes)
        {
            EngineManifestValidator.Validate(type);

            var attribute = type.GetCustomAttribute<EngineManifestAttribute>()!;
            var engine = (IEngine)Activator.CreateInstance(type)!;

            var metadata = new EngineMetadata(
                EngineName: attribute.EngineName,
                Tier: attribute.Tier,
                Kind: attribute.Kind,
                InputContract: attribute.InputContract,
                OutputEvents: attribute.OutputEvents.Select(t => t.Name).ToList().AsReadOnly()
            );

            _registry.Register(engine);
            _descriptors.Add(new EngineDescriptor(engine, metadata));
        }
    }

    public IReadOnlyList<EngineDescriptor> GetDescriptors() => _descriptors.AsReadOnly();

    public IReadOnlyList<EngineMetadata> GetManifests() =>
        _descriptors.Select(d => d.Metadata).ToList().AsReadOnly();
}
