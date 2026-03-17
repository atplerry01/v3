namespace Whycespace.Runtime.EventSchemaRegistry.Builder;

using System.Reflection;
using Whycespace.Runtime.EventSchemaRegistry.Discovery;
using Whycespace.Runtime.EventSchemaRegistry.Models;
using Whycespace.Runtime.EventSchemaRegistry.Registry;
using Whycespace.Runtime.EventSchemaRegistry.Validation;

public sealed class EventRegistryBuilder
{
    private readonly List<Assembly> _assemblies = [];
    private readonly List<EventDescriptor> _explicitDescriptors = [];
    private bool _validateCompatibility = true;

    public EventRegistryBuilder AddAssembly(Assembly assembly)
    {
        _assemblies.Add(assembly);
        return this;
    }

    public EventRegistryBuilder AddAssemblies(IEnumerable<Assembly> assemblies)
    {
        _assemblies.AddRange(assemblies);
        return this;
    }

    public EventRegistryBuilder AddDescriptor(EventDescriptor descriptor)
    {
        _explicitDescriptors.Add(descriptor);
        return this;
    }

    public EventRegistryBuilder SkipCompatibilityValidation()
    {
        _validateCompatibility = false;
        return this;
    }

    public EventRegistry Build()
    {
        var discovery = new EventSchemaDiscovery();
        var validator = new EventRegistryValidator();

        var discovered = discovery.DiscoverFromAssemblies(_assemblies);

        foreach (var descriptor in discovered)
        {
            validator.ValidateType(descriptor.EventType);
        }

        var allDescriptors = new List<EventDescriptor>(discovered);
        allDescriptors.AddRange(_explicitDescriptors);

        validator.ValidateUniqueIds(allDescriptors);

        if (_validateCompatibility)
        {
            validator.ValidateVersionCompatibility(allDescriptors);
        }

        var registry = new EventRegistry(validator);

        foreach (var descriptor in allDescriptors)
        {
            registry.Register(descriptor);
        }

        return registry;
    }
}
