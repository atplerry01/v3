namespace Whycespace.Systems.WhyceID.Stores;

using global::System.Collections.Concurrent;
using Whycespace.Systems.WhyceID.Models;

public sealed class IdentityServiceStore
{
    private readonly ConcurrentDictionary<Guid, IdentityService> _services = new();

    public void Register(IdentityService service)
    {
        _services[service.ServiceId] = service;
    }

    public IdentityService Get(Guid serviceId)
    {
        if (!_services.TryGetValue(serviceId, out var service))
            throw new KeyNotFoundException($"Service not found: {serviceId}");

        return service;
    }

    public bool Authenticate(Guid serviceId, string secret)
    {
        if (!_services.TryGetValue(serviceId, out var service))
            return false;

        if (service.Revoked)
            return false;

        return service.Secret == secret;
    }

    public void Revoke(Guid serviceId)
    {
        if (_services.TryGetValue(serviceId, out var service))
        {
            _services[serviceId] = service with { Revoked = true };
        }
    }

    public IReadOnlyCollection<IdentityService> GetAll()
    {
        return _services.Values.ToList();
    }
}
