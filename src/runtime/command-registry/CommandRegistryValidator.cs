using System.Reflection;
using Whycespace.Contracts.Commands;

namespace Whycespace.Runtime.CommandRegistry;

public sealed class CommandRegistryValidator
{
    public void ValidateType(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);

        var errors = new List<string>();

        if (!type.IsClass)
            errors.Add($"Type '{type.Name}' must be a class.");

        if (type.IsAbstract)
            errors.Add($"Type '{type.Name}' must not be abstract.");

        if (!typeof(ICommand).IsAssignableFrom(type))
            errors.Add($"Type '{type.Name}' must implement ICommand.");

        var attribute = type.GetCustomAttribute<CommandManifestAttribute>();
        if (attribute is null)
            errors.Add($"Type '{type.Name}' must be decorated with CommandManifestAttribute.");

        if (!IsImmutable(type))
            errors.Add($"Type '{type.Name}' must be immutable (record or init-only properties).");

        if (errors.Count > 0)
            throw new CommandRegistryException(attribute?.CommandId ?? type.Name, errors);
    }

    public void ValidateUniqueIds(IEnumerable<CommandDescriptor> descriptors)
    {
        var duplicates = descriptors
            .GroupBy(d => (d.CommandId, d.Version))
            .Where(g => g.Count() > 1)
            .Select(g => $"Duplicate command registration: '{g.Key.CommandId}' v{g.Key.Version}")
            .ToList();

        if (duplicates.Count > 0)
            throw new CommandRegistryException("command-registry", duplicates);
    }

    public void ValidateVersionCompatibility(IEnumerable<CommandDescriptor> descriptors)
    {
        var grouped = descriptors
            .GroupBy(d => d.CommandId)
            .Where(g => g.Count() > 1);

        var errors = new List<string>();

        foreach (var group in grouped)
        {
            var versions = group.OrderBy(d => d.Version).ToList();

            for (var i = 1; i < versions.Count; i++)
            {
                var older = versions[i - 1];
                var newer = versions[i];

                if (!newer.Version.IsCompatibleWith(older.Version))
                    continue;

                var removedProperties = older.Properties
                    .Where(op => newer.Properties.All(np => np.Name != op.Name))
                    .Select(op => op.Name)
                    .ToList();

                if (removedProperties.Count > 0)
                    errors.Add($"Command '{group.Key}' v{newer.Version} removed properties: {string.Join(", ", removedProperties)}");

                var addedRequired = newer.Properties
                    .Where(np => np.IsRequired && older.Properties.All(op => op.Name != np.Name))
                    .Select(np => np.Name)
                    .ToList();

                if (addedRequired.Count > 0)
                    errors.Add($"Command '{group.Key}' v{newer.Version} added required properties: {string.Join(", ", addedRequired)}");
            }
        }

        if (errors.Count > 0)
            throw new CommandRegistryException("command-registry", errors);
    }

    private static bool IsImmutable(Type type)
    {
        if (IsRecord(type))
            return true;

        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        return properties.All(IsInitOnly);
    }

    private static bool IsRecord(Type type) =>
        type.GetMethod("<Clone>$") is not null;

    private static bool IsInitOnly(PropertyInfo property)
    {
        var setMethod = property.GetSetMethod();
        if (setMethod is null)
            return true;

        return setMethod.ReturnParameter
            .GetRequiredCustomModifiers()
            .Any(m => m.FullName == "System.Runtime.CompilerServices.IsExternalInit");
    }
}
