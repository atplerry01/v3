using Whycespace.EventFabric.Contracts;

namespace Whycespace.EventFabric.Registry;

public sealed class EventRegistry : IEventRegistry
{
    private readonly Dictionary<string, EventRegistration> _registrations = new();

    public void Register(EventRegistration registration)
    {
        if (_registrations.ContainsKey(registration.EventType))
            throw new InvalidOperationException(
                $"Event '{registration.EventType}' is already registered.");

        _registrations[registration.EventType] = registration;
    }

    public bool IsRegistered(string eventType)
    {
        return _registrations.ContainsKey(eventType);
    }

    public string? GetTopic(string eventType)
    {
        return _registrations.TryGetValue(eventType, out var reg) ? reg.Topic : null;
    }

    public string? GetOwningCluster(string eventType)
    {
        return _registrations.TryGetValue(eventType, out var reg) ? reg.OwningCluster : null;
    }

    public EventRegistration? GetRegistration(string eventType)
    {
        return _registrations.TryGetValue(eventType, out var reg) ? reg : null;
    }

    public IReadOnlyCollection<EventRegistration> GetAll()
    {
        return _registrations.Values.ToList().AsReadOnly();
    }
}
