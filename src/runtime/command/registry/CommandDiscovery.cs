using System.Reflection;

namespace Whycespace.CommandSystem.Registry;

public sealed class CommandDiscovery
{
    public IReadOnlyList<CommandDescriptor> DiscoverFromAssemblies(IEnumerable<Assembly> assemblies)
    {
        ArgumentNullException.ThrowIfNull(assemblies);

        return assemblies
            .SelectMany(DiscoverFromAssembly)
            .ToList()
            .AsReadOnly();
    }

    public IReadOnlyList<CommandDescriptor> DiscoverFromAssembly(Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);

        Type[] types;
        try
        {
            types = assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            types = ex.Types.Where(t => t is not null).ToArray()!;
        }

        return types
            .Where(t => t.IsClass && !t.IsAbstract)
            .Select(t => (Type: t, Attribute: t.GetCustomAttribute<CommandManifestAttribute>()))
            .Where(x => x.Attribute is not null)
            .Select(x => BuildDescriptor(x.Type, x.Attribute!))
            .ToList()
            .AsReadOnly();
    }

    private static CommandDescriptor BuildDescriptor(Type type, CommandManifestAttribute attribute)
    {
        var properties = type
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Select(p => new CommandPropertyDescriptor(
                p.Name,
                GetFriendlyTypeName(p.PropertyType),
                !IsNullable(p.PropertyType)
            ))
            .ToList()
            .AsReadOnly();

        return new CommandDescriptor(
            attribute.CommandId,
            attribute.Domain,
            new CommandVersion(attribute.Version),
            type,
            attribute.Description,
            properties
        );
    }

    private static string GetFriendlyTypeName(Type type)
    {
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
            return $"{GetFriendlyTypeName(type.GetGenericArguments()[0])}?";

        if (type.IsGenericType)
        {
            var name = type.Name[..type.Name.IndexOf('`')];
            var args = string.Join(", ", type.GetGenericArguments().Select(GetFriendlyTypeName));
            return $"{name}<{args}>";
        }

        return type.Name;
    }

    private static bool IsNullable(Type type)
    {
        if (!type.IsValueType) return true;
        return Nullable.GetUnderlyingType(type) is not null;
    }
}
