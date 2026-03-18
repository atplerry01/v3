namespace Whycespace.Runtime.EngineMetadata.Registry;

using System.Reflection;
using Whycespace.Runtime.EngineMetadata.Discovery;
using Whycespace.Runtime.EngineMetadata.Models;
using Whycespace.Runtime.EngineMetadata.Validation;

public sealed class EngineRegistryBuilder
{
    private readonly List<Assembly> _assemblies = new();
    private readonly List<EngineRegistryDescriptor> _descriptors = new();

    public EngineRegistryBuilder AddAssembly(Assembly assembly)
    {
        _assemblies.Add(assembly);
        return this;
    }

    public EngineRegistryBuilder AddAssemblies(IEnumerable<Assembly> assemblies)
    {
        _assemblies.AddRange(assemblies);
        return this;
    }

    public EngineRegistryBuilder AddDescriptor(EngineRegistryDescriptor descriptor)
    {
        _descriptors.Add(descriptor);
        return this;
    }

    public EngineRegistry Build()
    {
        var discovered = EngineDiscovery.DiscoverEngines(_assemblies);

        var all = new List<EngineRegistryDescriptor>(_descriptors.Count + discovered.Count);
        all.AddRange(_descriptors);
        all.AddRange(discovered);

        EngineRegistryValidator.ValidateUniqueness(all);

        return new EngineRegistry(all.AsReadOnly());
    }
}
