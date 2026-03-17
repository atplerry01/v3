namespace Whycespace.Runtime.EventFabricGuard;

using Whycespace.Runtime.EventSchemaRegistry.Exceptions;
using Whycespace.Runtime.EventSchemaRegistry.Models;
using Whycespace.Runtime.EventSchemaRegistry.Registry;

public sealed class EventFabricGuard
{
    private readonly EventRegistry _registry;

    public EventFabricGuard(EventRegistry registry)
    {
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
    }

    public void Validate(object eventInstance)
    {
        ArgumentNullException.ThrowIfNull(eventInstance);

        var eventType = eventInstance.GetType();

        var descriptor = _registry.GetDescriptorForType(eventType);
        if (descriptor is null)
        {
            throw new EventFabricGuardException(
                $"Event type '{eventType.FullName}' is not registered in the event schema registry.",
                eventType.Name);
        }

        ValidateSchemaExists(descriptor, eventType);
        ValidateImmutability(eventType);
        ValidateDomainCorrectness(descriptor);

        try
        {
            _registry.Validate(eventInstance);
        }
        catch (EventRegistryException ex)
        {
            throw new EventFabricGuardException(
                $"Event validation failed for '{descriptor.EventId}': {ex.Message}",
                descriptor.EventId,
                ex);
        }
    }

    public void Validate(EventPublishContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        Validate(context.EventInstance);
    }

    private static void ValidateSchemaExists(EventDescriptor descriptor, Type eventType)
    {
        if (descriptor.EventType != eventType)
        {
            throw new EventFabricGuardException(
                $"Event type mismatch: expected '{descriptor.EventType.FullName}' but got '{eventType.FullName}'.",
                descriptor.EventId);
        }
    }

    private static void ValidateImmutability(Type eventType)
    {
        if (!eventType.IsClass)
            return;

        var isRecord = eventType.GetMethods().Any(m => m.Name == "<Clone>$");
        if (isRecord)
            return;

        var properties = eventType.GetProperties();
        foreach (var prop in properties)
        {
            if (prop.CanWrite)
            {
                var setter = prop.SetMethod;
                if (setter is not null && !setter.ReturnParameter.GetRequiredCustomModifiers()
                    .Contains(typeof(System.Runtime.CompilerServices.IsExternalInit)))
                {
                    throw new EventFabricGuardException(
                        $"Event type '{eventType.FullName}' is not immutable. Property '{prop.Name}' has a public setter.",
                        eventType.Name);
                }
            }
        }
    }

    private static void ValidateDomainCorrectness(EventDescriptor descriptor)
    {
        if (string.IsNullOrWhiteSpace(descriptor.Domain))
        {
            throw new EventFabricGuardException(
                $"Event '{descriptor.EventId}' has no domain assigned.",
                descriptor.EventId);
        }
    }
}
