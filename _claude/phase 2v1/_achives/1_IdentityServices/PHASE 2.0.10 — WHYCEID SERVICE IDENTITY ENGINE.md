# WHYCESPACE WBSM v3

# PHASE 2.0.10 — WHYCEID SERVICE IDENTITY ENGINE
(System + Engine Architecture)

You are implementing the **Service Identity subsystem of WhyceID**.

Service identities represent **system actors within Whycespace**.

These identities are used by:

• system services  
• automated workflows  
• internal APIs  
• integration clients  
• engine service accounts  

Service identities allow internal systems to authenticate and authorize themselves securely.

Follow WBSM rules:

SYSTEM → state and registries  
ENGINE → deterministic execution logic  

Engines must remain **stateless**.

---

# OBJECTIVES

Implement:

SYSTEM COMPONENT

• ServiceIdentity  
• ServiceIdentityRegistry  

ENGINE COMPONENT

• RegisterServiceIdentityEngine  
• AuthenticateServiceIdentityEngine  
• RevokeServiceIdentityEngine  

Also implement:

• Commands  
• Events  
• Unit tests  

---

# SYSTEM MODULE LOCATION

Extend:

```
src/system/upstream/WhyceID/
```

Create folder:

```
service/
```

Structure:

```
service/

├── ServiceIdentity.cs
└── ServiceIdentityRegistry.cs
```

---

# SERVICE IDENTITY MODEL

Create:

```
service/ServiceIdentity.cs
```

```csharp
public sealed class ServiceIdentity
{
    public string ServiceId { get; }

    public string Name { get; }

    public string SecretHash { get; }

    public DateTime CreatedAt { get; }

    public bool Active { get; private set; }

    public ServiceIdentity(
        string serviceId,
        string name,
        string secretHash)
    {
        ServiceId = serviceId;
        Name = name;
        SecretHash = secretHash;
        CreatedAt = DateTime.UtcNow;
        Active = true;
    }

    public void Revoke()
    {
        Active = false;
    }
}
```

---

# SERVICE IDENTITY REGISTRY

Create:

```
service/ServiceIdentityRegistry.cs
```

```csharp
public sealed class ServiceIdentityRegistry
{
    private readonly ConcurrentDictionary<string, ServiceIdentity> _services
        = new();

    public void Register(ServiceIdentity service)
    {
        if (!_services.TryAdd(service.ServiceId, service))
            throw new InvalidOperationException("Service already exists");
    }

    public ServiceIdentity Get(string serviceId)
    {
        if (!_services.TryGetValue(serviceId, out var service))
            throw new KeyNotFoundException("Service not found");

        return service;
    }

    public bool Exists(string serviceId)
    {
        return _services.ContainsKey(serviceId);
    }
}
```

---

# COMMANDS

Create:

```
commands/RegisterServiceIdentityCommand.cs
```

```csharp
public sealed record RegisterServiceIdentityCommand(
    string ServiceId,
    string Name,
    string Secret);
```

---

Create:

```
commands/AuthenticateServiceIdentityCommand.cs
```

```csharp
public sealed record AuthenticateServiceIdentityCommand(
    string ServiceId,
    string Secret);
```

---

Create:

```
commands/RevokeServiceIdentityCommand.cs
```

```csharp
public sealed record RevokeServiceIdentityCommand(
    string ServiceId);
```

---

# EVENTS

Create:

```
events/ServiceIdentityRegisteredEvent.cs
```

```csharp
public sealed record ServiceIdentityRegisteredEvent(
    string ServiceId,
    DateTime CreatedAt);
```

---

Create:

```
events/ServiceIdentityAuthenticatedEvent.cs
```

```csharp
public sealed record ServiceIdentityAuthenticatedEvent(
    string ServiceId,
    DateTime AuthenticatedAt);
```

---

Create:

```
events/ServiceIdentityRevokedEvent.cs
```

```csharp
public sealed record ServiceIdentityRevokedEvent(
    string ServiceId,
    DateTime RevokedAt);
```

---

# ENGINE LOCATION

Create engines in:

```
src/engines/T0U_Constitutional/WhyceIdentity/
```

---

# SECRET HASH UTILITY

Create:

```
ServiceSecretHasher.cs
```

```csharp
public static class ServiceSecretHasher
{
    public static string Hash(string secret)
    {
        using var sha = SHA256.Create();

        var bytes = Encoding.UTF8.GetBytes(secret);

        var hash = sha.ComputeHash(bytes);

        return Convert.ToBase64String(hash);
    }
}
```

---

# REGISTER SERVICE IDENTITY ENGINE

Create:

```
RegisterServiceIdentityEngine.cs
```

```csharp
public sealed class RegisterServiceIdentityEngine
{
    public ServiceIdentityRegisteredEvent Execute(
        RegisterServiceIdentityCommand command,
        ServiceIdentityRegistry registry)
    {
        if (registry.Exists(command.ServiceId))
            throw new InvalidOperationException("Service already exists");

        var hash = ServiceSecretHasher.Hash(command.Secret);

        var service = new ServiceIdentity(
            command.ServiceId,
            command.Name,
            hash);

        registry.Register(service);

        return new ServiceIdentityRegisteredEvent(
            command.ServiceId,
            service.CreatedAt);
    }
}
```

---

# AUTHENTICATE SERVICE ENGINE

Create:

```
AuthenticateServiceIdentityEngine.cs
```

```csharp
public sealed class AuthenticateServiceIdentityEngine
{
    public ServiceIdentityAuthenticatedEvent Execute(
        AuthenticateServiceIdentityCommand command,
        ServiceIdentityRegistry registry)
    {
        var service = registry.Get(command.ServiceId);

        if (!service.Active)
            throw new UnauthorizedAccessException("Service revoked");

        var hash = ServiceSecretHasher.Hash(command.Secret);

        if (service.SecretHash != hash)
            throw new UnauthorizedAccessException("Invalid secret");

        return new ServiceIdentityAuthenticatedEvent(
            command.ServiceId,
            DateTime.UtcNow);
    }
}
```

---

# REVOKE SERVICE ENGINE

Create:

```
RevokeServiceIdentityEngine.cs
```

```csharp
public sealed class RevokeServiceIdentityEngine
{
    public ServiceIdentityRevokedEvent Execute(
        RevokeServiceIdentityCommand command,
        ServiceIdentityRegistry registry)
    {
        var service = registry.Get(command.ServiceId);

        service.Revoke();

        return new ServiceIdentityRevokedEvent(
            command.ServiceId,
            DateTime.UtcNow);
    }
}
```

---

# UNIT TESTS

Create:

```
tests/Whycespace.ServiceIdentity.Tests/
```

Tests:

```
ServiceRegistrationTests
ServiceAuthenticationTests
ServiceRevocationTests
```

Example:

```csharp
[Fact]
public void ServiceIdentity_ShouldRegister()
{
    var registry = new ServiceIdentityRegistry();

    var engine = new RegisterServiceIdentityEngine();

    var result = engine.Execute(
        new RegisterServiceIdentityCommand(
            "atlas-service",
            "WhyceAtlas",
            "secret"),
        registry);

    Assert.NotNull(result);
}
```

---

# BUILD VALIDATION

Run:

```
dotnet build
```

Expected:

```
Build succeeded
0 errors
0 warnings
```

---

# SUCCESS CRITERIA

Service identities registered  
Secrets hashed correctly  
Service authentication works  
Service revocation works  
Unit tests pass  

---

# END OF PHASE 2.0.10