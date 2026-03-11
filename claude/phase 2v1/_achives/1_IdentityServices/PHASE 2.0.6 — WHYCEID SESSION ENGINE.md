# WHYCESPACE WBSM v3

# PHASE 2.0.6 — WHYCEID SESSION ENGINE
(System + Engine Architecture)

You are implementing the **session subsystem of WhyceID**.

Sessions represent **authenticated login contexts** for identities.

A session contains:

• identity id  
• session token  
• creation time  
• expiration time  

Sessions allow the system to validate whether a request is made by an **authenticated identity**.

Follow WBSM rules:

SYSTEM → state and registries  
ENGINE → deterministic execution logic  

Engines must remain **stateless**.

---

# OBJECTIVES

Implement:

SYSTEM COMPONENT

• SessionRecord  
• SessionRegistry  

ENGINE COMPONENT

• SessionCreateEngine  
• SessionValidateEngine  
• SessionTerminateEngine  

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
session/
```

Structure:

```
session/

├── SessionRecord.cs
└── SessionRegistry.cs
```

---

# SESSION RECORD

Create:

```
session/SessionRecord.cs
```

```csharp
public sealed class SessionRecord
{
    public string Token { get; }

    public Guid IdentityId { get; }

    public DateTime CreatedAt { get; }

    public DateTime ExpiresAt { get; }

    public SessionRecord(
        string token,
        Guid identityId,
        DateTime createdAt,
        DateTime expiresAt)
    {
        Token = token;
        IdentityId = identityId;
        CreatedAt = createdAt;
        ExpiresAt = expiresAt;
    }

    public bool IsExpired()
    {
        return DateTime.UtcNow > ExpiresAt;
    }
}
```

---

# SESSION REGISTRY

Create:

```
session/SessionRegistry.cs
```

```csharp
public sealed class SessionRegistry
{
    private readonly ConcurrentDictionary<string, SessionRecord> _sessions
        = new();

    public void Register(SessionRecord session)
    {
        _sessions[session.Token] = session;
    }

    public SessionRecord Get(string token)
    {
        if (!_sessions.TryGetValue(token, out var session))
            throw new KeyNotFoundException("Session not found");

        return session;
    }

    public void Remove(string token)
    {
        _sessions.TryRemove(token, out _);
    }

    public bool Exists(string token)
    {
        return _sessions.ContainsKey(token);
    }
}
```

---

# COMMANDS

Create:

```
commands/CreateSessionCommand.cs
```

```csharp
public sealed record CreateSessionCommand(
    Guid IdentityId);
```

---

Create:

```
commands/ValidateSessionCommand.cs
```

```csharp
public sealed record ValidateSessionCommand(
    string Token);
```

---

Create:

```
commands/TerminateSessionCommand.cs
```

```csharp
public sealed record TerminateSessionCommand(
    string Token);
```

---

# EVENTS

Create:

```
events/SessionCreatedEvent.cs
```

```csharp
public sealed record SessionCreatedEvent(
    Guid IdentityId,
    string Token,
    DateTime CreatedAt,
    DateTime ExpiresAt);
```

---

Create:

```
events/SessionValidatedEvent.cs
```

```csharp
public sealed record SessionValidatedEvent(
    Guid IdentityId,
    string Token,
    DateTime ValidatedAt);
```

---

Create:

```
events/SessionTerminatedEvent.cs
```

```csharp
public sealed record SessionTerminatedEvent(
    string Token,
    DateTime TerminatedAt);
```

---

# ENGINE LOCATION

Create engines in:

```
src/engines/T0U_Constitutional/WhyceIdentity/
```

---

# SESSION CREATE ENGINE

Create:

```
SessionCreateEngine.cs
```

```csharp
public sealed class SessionCreateEngine
{
    public SessionCreatedEvent Execute(
        CreateSessionCommand command,
        SessionRegistry registry)
    {
        var token = Guid.NewGuid().ToString();

        var created = DateTime.UtcNow;

        var expires = created.AddHours(12);

        var session = new SessionRecord(
            token,
            command.IdentityId,
            created,
            expires);

        registry.Register(session);

        return new SessionCreatedEvent(
            command.IdentityId,
            token,
            created,
            expires);
    }
}
```

---

# SESSION VALIDATE ENGINE

Create:

```
SessionValidateEngine.cs
```

```csharp
public sealed class SessionValidateEngine
{
    public SessionValidatedEvent Execute(
        ValidateSessionCommand command,
        SessionRegistry registry)
    {
        var session = registry.Get(command.Token);

        if (session.IsExpired())
            throw new UnauthorizedAccessException("Session expired");

        return new SessionValidatedEvent(
            session.IdentityId,
            session.Token,
            DateTime.UtcNow);
    }
}
```

---

# SESSION TERMINATE ENGINE

Create:

```
SessionTerminateEngine.cs
```

```csharp
public sealed class SessionTerminateEngine
{
    public SessionTerminatedEvent Execute(
        TerminateSessionCommand command,
        SessionRegistry registry)
    {
        registry.Remove(command.Token);

        return new SessionTerminatedEvent(
            command.Token,
            DateTime.UtcNow);
    }
}
```

---

# UNIT TESTS

Create:

```
tests/Whycespace.Session.Tests/
```

Tests:

```
SessionCreationTests
SessionValidationTests
SessionTerminationTests
```

Example:

```csharp
[Fact]
public void SessionCreation_ShouldGenerateToken()
{
    var registry = new SessionRegistry();

    var engine = new SessionCreateEngine();

    var id = Guid.NewGuid();

    var result = engine.Execute(
        new CreateSessionCommand(id),
        registry);

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

Sessions created successfully  
Sessions validated correctly  
Expired sessions rejected  
Sessions terminated correctly  
Unit tests pass  

---

# END OF PHASE 2.0.6