namespace Whycespace.Runtime.EventSchemaRegistry.Registry;

using System.Reflection;
using Whycespace.Runtime.EventSchemaRegistry.Attributes;
using Whycespace.Runtime.EventSchemaRegistry.Exceptions;
using Whycespace.Runtime.EventSchemaRegistry.Models;
using Whycespace.Runtime.EventSchemaRegistry.Snapshot;
using Whycespace.Runtime.EventSchemaRegistry.Validation;

public sealed class EventRegistry
{
    private readonly Dictionary<string, List<EventDescriptor>> _descriptors = new();
    private readonly Dictionary<Type, EventDescriptor> _typeIndex = new();
    private readonly EventRegistryValidator _validator;

    internal EventRegistry(EventRegistryValidator validator)
    {
        _validator = validator;
    }

    internal void Register(EventDescriptor descriptor)
    {
        if (!_descriptors.TryGetValue(descriptor.EventId, out var versions))
        {
            versions = [];
            _descriptors[descriptor.EventId] = versions;
        }

        versions.Add(descriptor);
        _typeIndex[descriptor.EventType] = descriptor;
    }

    public EventDescriptor? GetDescriptor(string eventId)
    {
        if (!_descriptors.TryGetValue(eventId, out var versions) || versions.Count == 0)
            return null;

        return versions.OrderByDescending(v => v.Version).First();
    }

    public EventDescriptor? GetDescriptor(string eventId, EventSchemaVersion version)
    {
        if (!_descriptors.TryGetValue(eventId, out var versions))
            return null;

        return versions.FirstOrDefault(v => v.Version == version);
    }

    public EventDescriptor? GetDescriptorForType(Type eventType)
    {
        return _typeIndex.GetValueOrDefault(eventType);
    }

    public IReadOnlyList<EventDescriptor> GetAllDescriptors()
    {
        return _descriptors.Values.SelectMany(v => v).ToList();
    }

    public IReadOnlyList<EventDescriptor> GetByDomain(string domain)
    {
        return _descriptors.Values
            .SelectMany(v => v)
            .Where(d => string.Equals(d.Domain, domain, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    public IReadOnlyList<string> GetDomains()
    {
        return _descriptors.Values
            .SelectMany(v => v)
            .Select(d => d.Domain)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(d => d)
            .ToList();
    }

    public void Validate(object eventInstance)
    {
        var type = eventInstance.GetType();
        var attribute = type.GetCustomAttribute<EventSchemaAttribute>();

        if (attribute is null)
        {
            throw new EventRegistryException(
                type.Name,
                $"Type '{type.Name}' is not a registered event (missing [EventSchema] attribute).");
        }

        if (!_typeIndex.ContainsKey(type))
        {
            throw new EventRegistryException(
                attribute.EventId,
                $"Event '{attribute.EventId}' is not registered in this registry.");
        }

        var errors = _validator.Validate(eventInstance);
        if (errors.Count > 0)
        {
            throw new EventRegistryException(attribute.EventId, errors.ToList());
        }
    }

    public bool IsRegistered(string eventId) => _descriptors.ContainsKey(eventId);

    public bool IsRegistered(Type eventType) => _typeIndex.ContainsKey(eventType);

    public int Count => _typeIndex.Count;

    public EventRegistrySnapshot CreateSnapshot()
    {
        var domainEntries = GetDomains()
            .Select(domain => new DomainEventGroup(
                Domain: domain,
                Events: GetByDomain(domain)
            ))
            .ToList();

        return new EventRegistrySnapshot(
            GeneratedAt: DateTimeOffset.UtcNow,
            TotalEventCount: Count,
            Domains: domainEntries
        );
    }
}
