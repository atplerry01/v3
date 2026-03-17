namespace Whycespace.Runtime.EngineRegistry;

using System.Reflection;

public sealed class EngineRegistryBuilder
{
    private readonly List<Assembly> _assemblies = new();
    private readonly List<EngineDescriptor> _descriptors = new();

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

    public EngineRegistryBuilder AddDescriptor(EngineDescriptor descriptor)
    {
        _descriptors.Add(descriptor);
        return this;
    }

    public EngineRegistry Build()
    {
        var discovered = EngineDiscovery.DiscoverEngines(_assemblies);

        var all = new List<EngineDescriptor>(_descriptors.Count + discovered.Count);
        all.AddRange(_descriptors);
        all.AddRange(discovered);

        EngineRegistryValidator.ValidateUniqueness(all);

        return new EngineRegistry(all.AsReadOnly());
    }
}
