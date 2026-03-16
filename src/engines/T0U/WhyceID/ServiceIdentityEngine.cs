namespace Whycespace.Engines.T0U.WhyceID;

using Whycespace.Systems.WhyceID.Models;
using Whycespace.Systems.WhyceID.Stores;

public sealed class ServiceIdentityEngine
{
    private readonly IdentityServiceStore _store;

    public ServiceIdentityEngine(IdentityServiceStore store)
    {
        _store = store;
    }

    public IdentityService RegisterService(
        string name,
        string type,
        string secret)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Service name cannot be empty.");

        if (string.IsNullOrWhiteSpace(type))
            throw new ArgumentException("Service type cannot be empty.");

        if (string.IsNullOrWhiteSpace(secret))
            throw new ArgumentException("Secret cannot be empty.");

        var service = new IdentityService(
            Guid.NewGuid(),
            name,
            type,
            secret,
            DateTime.UtcNow,
            false
        );

        _store.Register(service);

        return service;
    }

    public bool AuthenticateService(Guid serviceId, string secret)
    {
        return _store.Authenticate(serviceId, secret);
    }

    public void RevokeService(Guid serviceId)
    {
        _store.Revoke(serviceId);
    }

    public IReadOnlyCollection<IdentityService> GetServices()
    {
        return _store.GetAll();
    }
}
