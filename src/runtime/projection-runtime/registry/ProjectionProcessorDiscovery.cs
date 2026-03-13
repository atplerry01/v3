using System.Reflection;

namespace Whycespace.ProjectionRuntime.Registry;

/// <summary>
/// Scans assemblies for projection processors marked with [ProjectionProcessor]
/// and registers them with the ProjectionRegistry.
/// </summary>
public sealed class ProjectionProcessorDiscovery
{
    private const string ProcessorInterfaceName = "IProjectionProcessor`1";

    private readonly ProjectionRegistry _registry;
    private readonly IServiceProvider _serviceProvider;
    private readonly HashSet<string> _registeredProjections = new();

    public ProjectionProcessorDiscovery(
        ProjectionRegistry registry,
        IServiceProvider serviceProvider)
    {
        _registry = registry;
        _serviceProvider = serviceProvider;
    }

    public int DiscoverAndRegister(Assembly assembly)
    {
        var processorTypes = ScanForProcessors(assembly);
        var registered = 0;

        foreach (var type in processorTypes)
        {
            ValidateProcessor(type);
            RegisterProcessor(type);
            registered++;
        }

        return registered;
    }

    private static IReadOnlyList<Type> ScanForProcessors(Assembly assembly)
    {
        return assembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false })
            .Where(t => t.GetCustomAttribute<ProjectionProcessorAttribute>() is not null)
            .ToList();
    }

    private static void ValidateProcessor(Type type)
    {
        var processorInterface = type.GetInterfaces()
            .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition().Name == ProcessorInterfaceName);

        if (processorInterface is null)
        {
            throw new InvalidOperationException(
                $"Type '{type.FullName}' is marked with [ProjectionProcessor] " +
                $"but does not implement IProjectionProcessor<TEvent>.");
        }
    }

    private void RegisterProcessor(Type type)
    {
        var instance = _serviceProvider.GetService(type)
            ?? ActivatorUtilities.CreateInstance(_serviceProvider, type);

        var projectionName = (string)type.GetProperty("ProjectionName")!.GetValue(instance)!;

        if (!_registeredProjections.Add(projectionName))
        {
            throw new InvalidOperationException(
                $"Duplicate projection processor '{projectionName}' detected on type '{type.FullName}'.");
        }

        var handledEventTypes = (IReadOnlyCollection<string>)type.GetProperty("HandledEventTypes")!.GetValue(instance)!;

        foreach (var eventType in handledEventTypes)
        {
            _registry.Register(eventType, projectionName);
        }
    }
}

internal static class ActivatorUtilities
{
    public static object CreateInstance(IServiceProvider provider, Type type)
    {
        var constructors = type.GetConstructors();

        if (constructors.Length == 0)
            return Activator.CreateInstance(type)!;

        var ctor = constructors
            .OrderByDescending(c => c.GetParameters().Length)
            .First();

        var parameters = ctor.GetParameters()
            .Select(p => provider.GetService(p.ParameterType)
                ?? throw new InvalidOperationException(
                    $"Cannot resolve parameter '{p.Name}' of type '{p.ParameterType.FullName}' " +
                    $"for projection processor '{type.FullName}'."))
            .ToArray();

        return ctor.Invoke(parameters);
    }
}
