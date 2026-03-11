# WHYCESPACE WBSM v3

# PHASE 2.0.3 — WHYCEID AUTHENTICATION ENGINE
(System + Engine Architecture)

You are implementing the **authentication subsystem of WhyceID**.

This subsystem manages:

• credential registration  
• password hashing  
• login validation  
• token issuance  

Authentication verifies that a user **possesses valid credentials for an identity**.

Follow WBSM architecture rules:

SYSTEM → state and registries  
ENGINE → deterministic execution logic  

Engines must remain **stateless**.

---

# OBJECTIVES

Implement:

SYSTEM COMPONENT

• CredentialRecord  
• CredentialRegistry  

ENGINE COMPONENT

• CredentialRegistrationEngine  
• AuthenticationEngine  

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
authentication/
```

Structure:

```
authentication/

├── CredentialRecord.cs
├── CredentialRegistry.cs
```

---

# CREDENTIAL RECORD

Create:

```
authentication/CredentialRecord.cs
```

```csharp
public sealed class CredentialRecord
{
    public Guid IdentityId { get; }

    public string PasswordHash { get; }

    public DateTime CreatedAt { get; }

    public CredentialRecord(Guid identityId, string passwordHash)
    {
        IdentityId = identityId;
        PasswordHash = passwordHash;
        CreatedAt = DateTime.UtcNow;
    }
}
```

---

# CREDENTIAL REGISTRY

Create:

```
authentication/CredentialRegistry.cs
```

```csharp
public sealed class CredentialRegistry
{
    private readonly ConcurrentDictionary<Guid, CredentialRecord> _credentials
        = new();

    public void Register(CredentialRecord record)
    {
        if (!_credentials.TryAdd(record.IdentityId, record))
            throw new InvalidOperationException("Credential already exists");
    }

    public CredentialRecord Get(Guid identityId)
    {
        if (!_credentials.TryGetValue(identityId, out var record))
            throw new KeyNotFoundException("Credential not found");

        return record;
    }

    public bool Exists(Guid identityId)
    {
        return _credentials.ContainsKey(identityId);
    }
}
```

---

# COMMANDS

Create:

```
commands/RegisterCredentialCommand.cs
```

```csharp
public sealed record RegisterCredentialCommand(
    Guid IdentityId,
    string Password);
```

---

Create:

```
commands/AuthenticateCommand.cs
```

```csharp
public sealed record AuthenticateCommand(
    Guid IdentityId,
    string Password);
```

---

# EVENTS

Create:

```
events/CredentialRegisteredEvent.cs
```

```csharp
public sealed record CredentialRegisteredEvent(
    Guid IdentityId,
    DateTime CreatedAt);
```

---

Create:

```
events/AuthenticationSucceededEvent.cs
```

```csharp
public sealed record AuthenticationSucceededEvent(
    Guid IdentityId,
    string Token,
    DateTime IssuedAt);
```

---

# ENGINE LOCATION

Create engines in:

```
src/engines/T0U_Constitutional/WhyceIdentity/
```

---

# PASSWORD HASH UTILITY

Create:

```
PasswordHasher.cs
```

```csharp
public static class PasswordHasher
{
    public static string Hash(string password)
    {
        using var sha = SHA256.Create();

        var bytes = Encoding.UTF8.GetBytes(password);

        var hash = sha.ComputeHash(bytes);

        return Convert.ToBase64String(hash);
    }
}
```

---

# CREDENTIAL REGISTRATION ENGINE

Create:

```
CredentialRegistrationEngine.cs
```

```csharp
public sealed class CredentialRegistrationEngine
{
    public CredentialRegisteredEvent Execute(
        RegisterCredentialCommand command,
        CredentialRegistry registry)
    {
        if (registry.Exists(command.IdentityId))
            throw new InvalidOperationException("Credential already exists");

        var hash = PasswordHasher.Hash(command.Password);

        var record = new CredentialRecord(
            command.IdentityId,
            hash);

        registry.Register(record);

        return new CredentialRegisteredEvent(
            command.IdentityId,
            record.CreatedAt);
    }
}
```

---

# AUTHENTICATION ENGINE

Create:

```
AuthenticationEngine.cs
```

```csharp
public sealed class AuthenticationEngine
{
    public AuthenticationSucceededEvent Execute(
        AuthenticateCommand command,
        CredentialRegistry registry)
    {
        var record = registry.Get(command.IdentityId);

        var hash = PasswordHasher.Hash(command.Password);

        if (record.PasswordHash != hash)
            throw new UnauthorizedAccessException("Invalid credentials");

        var token = Guid.NewGuid().ToString();

        return new AuthenticationSucceededEvent(
            command.IdentityId,
            token,
            DateTime.UtcNow);
    }
}
```

---

# UNIT TESTS

Create:

```
tests/Whycespace.Authentication.Tests/
```

Tests:

```
CredentialRegistrationTests
AuthenticationTests
CredentialRegistryTests
```

Example:

```csharp
[Fact]
public void Authentication_ShouldReturnToken()
{
    var registry = new CredentialRegistry();

    var regEngine = new CredentialRegistrationEngine();

    var authEngine = new AuthenticationEngine();

    var id = Guid.NewGuid();

    regEngine.Execute(new RegisterCredentialCommand(id, "password"), registry);

    var result = authEngine.Execute(new AuthenticateCommand(id, "password"), registry);

    Assert.NotNull(result.Token);
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

Credentials stored successfully  
Passwords hashed  
Authentication validates credentials  
Token returned on success  
Unit tests pass  

---

# END OF PHASE 2.0.3