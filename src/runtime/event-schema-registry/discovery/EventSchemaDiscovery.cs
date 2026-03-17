namespace Whycespace.Runtime.EventSchemaRegistry.Discovery;

using System.Reflection;
using Whycespace.Runtime.EventSchemaRegistry.Attributes;
using Whycespace.Runtime.EventSchemaRegistry.Models;

public sealed class EventSchemaDiscovery
{
    public IReadOnlyList<EventDescriptor> DiscoverFromAssemblies(IEnumerable<Assembly> assemblies)
    {
        var descriptors = new List<EventDescriptor>();

        foreach (var assembly in assemblies)
        {
            descriptors.AddRange(DiscoverFromAssembly(assembly));
        }

        return descriptors;
    }

    public IReadOnlyList<EventDescriptor> DiscoverFromAssembly(Assembly assembly)
    {
        var descriptors = new List<EventDescriptor>();

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
            if (!type.IsClass || type.IsAbstract)
                continue;

            var attribute = type.GetCustomAttribute<EventSchemaAttribute>();
            if (attribute is null)
                continue;

            var descriptor = BuildDescriptor(type, attribute);
            descriptors.Add(descriptor);
        }

        return descriptors;
    }

    private static EventDescriptor BuildDescriptor(Type type, EventSchemaAttribute attribute)
    {
        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Select(p => new EventPropertyDescriptor(
                Name: p.Name,
                TypeName: GetFriendlyTypeName(p.PropertyType),
                IsRequired: p.PropertyType.IsValueType && Nullable.GetUnderlyingType(p.PropertyType) is null
            ))
            .ToList();

        return new EventDescriptor(
            EventId: attribute.EventId,
            Domain: attribute.Domain,
            Version: new EventSchemaVersion(attribute.Version),
            EventType: type,
            Description: attribute.Description,
            Properties: properties
        );
    }

    private static string GetFriendlyTypeName(Type type)
    {
        if (Nullable.GetUnderlyingType(type) is { } underlying)
            return $"{GetFriendlyTypeName(underlying)}?";

        if (type.IsGenericType)
        {
            var name = type.Name[..type.Name.IndexOf('`')];
            var args = string.Join(", ", type.GetGenericArguments().Select(GetFriendlyTypeName));
            return $"{name}<{args}>";
        }

        return type.Name;
    }
}
