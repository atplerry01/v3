# WHYCESPACE WBSM v3
# PHASE 2.0.15 — WHYCEID SERVICE IDENTITY ENGINE

You are implementing **Phase 2.0.15 of the WhyceID System**.

This phase introduces **Service Identities**, which represent
machine or system identities used by services, runtimes, and
system components inside Whycespace.

Service identities allow secure service-to-service authentication
without using human identities.

Examples:

workflow engine
cluster microservice
external integration service
automation agent
system runtime component

---

# ARCHITECTURE RULES

Follow WBSM v3 doctrine.

ENGINE
stateless logic

SYSTEM
stateful storage

Service identities must be stored in **IdentityServiceStore**.

The engine must NOT persist state directly.

---

# SERVICE IDENTITY CONCEPT

Service identity properties:

service identity id
service name
service type
secret key
created timestamp
revoked flag

Service lifecycle:

register service
authenticate service
revoke service
retrieve service identity

---

# TARGET LOCATIONS

Engine:

src/engines/T0U/WhyceID/

Create:

ServiceIdentityEngine.cs

System Store:

src/system/upstream/WhyceID/stores/

Create:

IdentityServiceStore.cs

Model:

src/system/upstream/WhyceID/models/

Create:

IdentityService.cs

---

# SERVICE MODEL

Create:

models/IdentityService.cs

Implementation:

namespace Whycespace.System.Upstream.WhyceID.Models;

public sealed record IdentityService(
    Guid ServiceId,
    string Name,
    string Type,
    string Secret,
    DateTime CreatedAt,
    bool Revoked
);

Validation rules:

ServiceId must not be empty
Name must not be empty
Type must not be empty
Secret must not be empty

Examples:

Name: wss-runtime
Type: workflow-engine

Name: whyceproperty-api
Type: cluster-service

Name: payment-gateway-adapter
Type: integration

---

# SERVICE STORE

Create:

stores/IdentityServiceStore.cs

Purpose:

store service identities
retrieve service identities
validate service authentication
revoke service identities

Implementation:

using System.Collections.Concurrent;
using Whycespace.System.Upstream.WhyceID.Models;

namespace Whycespace.System.Upstream.WhyceID.Stores;

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

---

# SERVICE IDENTITY ENGINE

Create:

src/engines/T0U/WhyceID/ServiceIdentityEngine.cs

Dependencies:

IdentityServiceStore

Implementation:

using Whycespace.System.Upstream.WhyceID.Models;
using Whycespace.System.Upstream.WhyceID.Stores;

namespace Whycespace.Engines.T0U.WhyceID;

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

---

# TESTING

Create tests:

tests/engines/whyceid/

ServiceIdentityEngineTests.cs

Test scenarios:

service registered successfully
empty service name rejected
empty service type rejected
service authentication success
service authentication failure
revoked service rejected
service listing returns all services

---

# DEBUG ENDPOINT

Add debug endpoint:

GET /dev/services

Returns all registered service identities.

Add endpoint:

POST /dev/services/register

Input:

name
type
secret

Returns created service identity.

Add endpoint:

POST /dev/services/revoke

Input:

serviceId

Revokes service identity.

Only available in DEBUG mode.

---

# SUCCESS CRITERIA

Build must succeed.

Requirements:

0 warnings
0 errors
all tests passing

Engine must remain stateless.

Store must be thread-safe.

---

# NEXT PHASE

After this phase implement:

2.0.16 Federation Engine

Federation will allow identity interoperability with
external identity providers such as:

OAuth providers
enterprise SSO systems
government identity systems
partner organization identity providers