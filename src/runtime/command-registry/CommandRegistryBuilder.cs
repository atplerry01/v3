using System.Reflection;

namespace Whycespace.Runtime.CommandRegistry;

public sealed class CommandRegistryBuilder
{
    private readonly List<Assembly> _assemblies = [];
    private readonly List<CommandDescriptor> _explicitDescriptors = [];
    private bool _validateCompatibility = true;

    public CommandRegistryBuilder AddAssembly(Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);
        _assemblies.Add(assembly);
        return this;
    }

    public CommandRegistryBuilder AddAssemblies(IEnumerable<Assembly> assemblies)
    {
        ArgumentNullException.ThrowIfNull(assemblies);
        _assemblies.AddRange(assemblies);
        return this;
    }

    public CommandRegistryBuilder AddDescriptor(CommandDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        _explicitDescriptors.Add(descriptor);
        return this;
    }

    public CommandRegistryBuilder SkipCompatibilityValidation()
    {
        _validateCompatibility = false;
        return this;
    }

    public CommandRegistry Build()
    {
        var validator = new CommandRegistryValidator();
        var discovery = new CommandDiscovery();

        var discovered = discovery.DiscoverFromAssemblies(_assemblies);

        foreach (var descriptor in discovered)
            validator.ValidateType(descriptor.CommandType);

        var allDescriptors = discovered.Concat(_explicitDescriptors).ToList();

        validator.ValidateUniqueIds(allDescriptors);

        if (_validateCompatibility)
            validator.ValidateVersionCompatibility(allDescriptors);

        var registry = new CommandRegistry(validator);

        foreach (var descriptor in allDescriptors)
            registry.Register(descriptor);

        return registry;
    }
}
