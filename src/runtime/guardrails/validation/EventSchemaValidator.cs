namespace Whycespace.ArchitectureGuardrails.Validation;

using global::System.Reflection;
using Whycespace.ArchitectureGuardrails.Rules;

public sealed record EventValidationResult(
    string EventTypeName,
    bool IsValid,
    IReadOnlyList<string> Violations
);

public sealed class EventSchemaValidator
{
    private static readonly string[] RequiredProperties = { "EventId", "EventType", "Timestamp" };

    public EventValidationResult ValidateEventType(Type eventType)
    {
        var violations = new List<string>();
        var name = eventType.Name;

        // Events must be immutable — records or classes with init-only properties
        if (!IsRecordType(eventType))
            violations.Add($"{name}: Events should be sealed record types for immutability. [{ArchitectureRules.EventSourcingRequired}]");

        if (!eventType.IsSealed)
            violations.Add($"{name}: Event types must be sealed.");

        // Check required properties
        foreach (var propName in RequiredProperties)
        {
            var prop = eventType.GetProperty(propName, BindingFlags.Instance | BindingFlags.Public);
            if (prop is null)
            {
                violations.Add($"{name}: Missing required property '{propName}'. [{ArchitectureRules.StateMutationsEmitEvents}]");
            }
            else if (prop.CanWrite && prop.SetMethod is { } setter && !setter.ReturnParameter.GetRequiredCustomModifiers().Any(m => m.Name == "IsExternalInit"))
            {
                // Check if property has a public setter (not init-only)
                var setMethod = prop.GetSetMethod();
                if (setMethod is not null)
                    violations.Add($"{name}: Property '{propName}' must be immutable (init-only or no setter).");
            }
        }

        // Check Timestamp type
        var timestampProp = eventType.GetProperty("Timestamp");
        if (timestampProp is not null)
        {
            var validTimestampTypes = new[] { typeof(DateTime), typeof(DateTimeOffset) };
            if (!validTimestampTypes.Contains(timestampProp.PropertyType))
                violations.Add($"{name}: Timestamp must be DateTime or DateTimeOffset.");
        }

        // Check EventId type
        var eventIdProp = eventType.GetProperty("EventId");
        if (eventIdProp is not null && eventIdProp.PropertyType != typeof(Guid))
            violations.Add($"{name}: EventId must be of type Guid.");

        return new EventValidationResult(name, violations.Count == 0, violations);
    }

    public IReadOnlyList<EventValidationResult> ValidateEventTypes(Assembly assembly)
    {
        var eventTypes = assembly.GetTypes()
            .Where(t => t is { IsAbstract: false, IsInterface: false }
                        && HasEventProperties(t))
            .Select(ValidateEventType)
            .ToList();

        return eventTypes;
    }

    private static bool HasEventProperties(Type type)
    {
        return type.GetProperty("EventId") is not null
               && type.GetProperty("EventType") is not null
               && type.GetProperty("Timestamp") is not null;
    }

    private static bool IsRecordType(Type type)
    {
        // Records have a compiler-generated EqualityContract property or <Clone>$ method
        return type.GetMethod("<Clone>$") is not null;
    }
}
