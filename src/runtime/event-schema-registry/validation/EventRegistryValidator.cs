namespace Whycespace.Runtime.EventSchemaRegistry.Validation;

using System.Reflection;
using Whycespace.Runtime.EventSchemaRegistry.Attributes;
using Whycespace.Runtime.EventSchemaRegistry.Exceptions;
using Whycespace.Runtime.EventSchemaRegistry.Models;

public sealed class EventRegistryValidator
{
    public void ValidateType(Type type)
    {
        var errors = new List<string>();

        var attribute = type.GetCustomAttribute<EventSchemaAttribute>();
        if (attribute is null)
        {
            throw new EventRegistryException(
                type.Name,
                $"Type '{type.Name}' is missing [EventSchema] attribute.");
        }

        if (!type.IsClass)
            errors.Add($"Type '{type.Name}' must be a class.");

        if (type.IsAbstract)
            errors.Add($"Type '{type.Name}' must not be abstract.");

        ValidateImmutability(type, errors);

        if (errors.Count > 0)
            throw new EventRegistryException(attribute.EventId, errors);
    }

    public void ValidateUniqueIds(IReadOnlyList<EventDescriptor> descriptors)
    {
        var duplicates = descriptors
            .GroupBy(d => d.EventId)
            .Where(g => g.Count() > 1
                        && g.Select(d => d.Version).Distinct().Count() < g.Count())
            .ToList();

        foreach (var group in duplicates)
        {
            var versionDuplicates = group
                .GroupBy(d => d.Version)
                .Where(vg => vg.Count() > 1)
                .SelectMany(vg => vg.Select(d => d.EventType.FullName ?? d.EventType.Name));

            throw new EventRegistryException(
                group.Key,
                $"Duplicate event ID '{group.Key}' with same version found on types: {string.Join(", ", versionDuplicates)}.");
        }
    }

    public void ValidateVersionCompatibility(IReadOnlyList<EventDescriptor> descriptors)
    {
        var grouped = descriptors
            .GroupBy(d => d.EventId)
            .Where(g => g.Count() > 1);

        foreach (var group in grouped)
        {
            var ordered = group.OrderBy(d => d.Version).ToList();

            for (var i = 1; i < ordered.Count; i++)
            {
                var compatibility = EventSchemaCompatibility.Check(ordered[i - 1], ordered[i]);
                if (!compatibility.IsCompatible)
                {
                    throw new EventRegistryException(
                        group.Key,
                        compatibility.Issues.ToList());
                }
            }
        }
    }

    public IReadOnlyList<string> Validate(object eventInstance)
    {
        var errors = new List<string>();
        var type = eventInstance.GetType();

        var attribute = type.GetCustomAttribute<EventSchemaAttribute>();
        if (attribute is null)
        {
            errors.Add($"Instance of '{type.Name}' is not a registered event type (missing [EventSchema] attribute).");
            return errors;
        }

        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

        foreach (var prop in properties)
        {
            var value = prop.GetValue(eventInstance);
            if (value is null && !IsNullable(prop.PropertyType))
            {
                errors.Add($"Required property '{prop.Name}' on event '{attribute.EventId}' is null.");
            }
        }

        return errors;
    }

    private static void ValidateImmutability(Type type, List<string> errors)
    {
        // Records with init-only properties are immutable by convention
        if (IsRecord(type))
            return;

        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

        foreach (var prop in properties)
        {
            var setter = prop.GetSetMethod();
            if (setter is not null && !IsInitOnly(prop))
            {
                errors.Add($"Property '{prop.Name}' on '{type.Name}' has a public setter. Event properties must be immutable (use init or get-only).");
            }
        }
    }

    private static bool IsRecord(Type type)
    {
        return type.GetMethod("<Clone>$") is not null;
    }

    private static bool IsInitOnly(PropertyInfo property)
    {
        var setMethod = property.GetSetMethod();
        if (setMethod is null) return false;

        return setMethod.ReturnParameter
            .GetRequiredCustomModifiers()
            .Any(m => m.FullName == "System.Runtime.CompilerServices.IsExternalInit");
    }

    private static bool IsNullable(Type type)
    {
        return !type.IsValueType || Nullable.GetUnderlyingType(type) is not null;
    }
}
