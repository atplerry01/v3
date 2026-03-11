# WHYCESPACE WBSM v3

# PHASE 2.0.7 — WHYCEID DEVICE TRUST ENGINE
(System + Engine Architecture)

You are implementing the **Device Trust subsystem of WhyceID**.

Device Trust allows the system to recognize and track the devices used by identities.

This improves:

• login security  
• fraud prevention  
• device reputation  
• suspicious activity detection  

Each device registered in WhyceID will contain:

• identity id  
• device id  
• device fingerprint  
• registration time  
• trust status  

Follow WBSM rules:

SYSTEM → state and registries  
ENGINE → deterministic execution logic  

Engines must remain **stateless**.

---

# OBJECTIVES

Implement:

SYSTEM COMPONENT

• DeviceRecord  
• DeviceRegistry  

ENGINE COMPONENT

• DeviceRegisterEngine  
• DeviceTrustEngine  
• DeviceRevokeEngine  

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
device/
```

Structure:

```
device/

├── DeviceRecord.cs
└── DeviceRegistry.cs
```

---

# DEVICE RECORD

Create:

```
device/DeviceRecord.cs
```

```csharp
public sealed class DeviceRecord
{
    public string DeviceId { get; }

    public Guid IdentityId { get; }

    public string Fingerprint { get; }

    public bool Trusted { get; private set; }

    public DateTime RegisteredAt { get; }

    public DeviceRecord(
        string deviceId,
        Guid identityId,
        string fingerprint)
    {
        DeviceId = deviceId;
        IdentityId = identityId;
        Fingerprint = fingerprint;
        RegisteredAt = DateTime.UtcNow;
        Trusted = false;
    }

    public void Trust()
    {
        Trusted = true;
    }

    public void Revoke()
    {
        Trusted = false;
    }
}
```

---

# DEVICE REGISTRY

Create:

```
device/DeviceRegistry.cs
```

```csharp
public sealed class DeviceRegistry
{
    private readonly ConcurrentDictionary<string, DeviceRecord> _devices
        = new();

    public void Register(DeviceRecord device)
    {
        if (!_devices.TryAdd(device.DeviceId, device))
            throw new InvalidOperationException("Device already exists");
    }

    public DeviceRecord Get(string deviceId)
    {
        if (!_devices.TryGetValue(deviceId, out var device))
            throw new KeyNotFoundException("Device not found");

        return device;
    }

    public bool Exists(string deviceId)
    {
        return _devices.ContainsKey(deviceId);
    }
}
```

---

# COMMANDS

Create:

```
commands/RegisterDeviceCommand.cs
```

```csharp
public sealed record RegisterDeviceCommand(
    Guid IdentityId,
    string DeviceId,
    string Fingerprint);
```

---

Create:

```
commands/TrustDeviceCommand.cs
```

```csharp
public sealed record TrustDeviceCommand(
    string DeviceId);
```

---

Create:

```
commands/RevokeDeviceCommand.cs
```

```csharp
public sealed record RevokeDeviceCommand(
    string DeviceId);
```

---

# EVENTS

Create:

```
events/DeviceRegisteredEvent.cs
```

```csharp
public sealed record DeviceRegisteredEvent(
    Guid IdentityId,
    string DeviceId,
    DateTime RegisteredAt);
```

---

Create:

```
events/DeviceTrustedEvent.cs
```

```csharp
public sealed record DeviceTrustedEvent(
    string DeviceId,
    DateTime TrustedAt);
```

---

Create:

```
events/DeviceRevokedEvent.cs
```

```csharp
public sealed record DeviceRevokedEvent(
    string DeviceId,
    DateTime RevokedAt);
```

---

# ENGINE LOCATION

Create engines in:

```
src/engines/T0U_Constitutional/WhyceIdentity/
```

---

# DEVICE REGISTER ENGINE

Create:

```
DeviceRegisterEngine.cs
```

```csharp
public sealed class DeviceRegisterEngine
{
    public DeviceRegisteredEvent Execute(
        RegisterDeviceCommand command,
        DeviceRegistry registry)
    {
        if (registry.Exists(command.DeviceId))
            throw new InvalidOperationException("Device already registered");

        var device = new DeviceRecord(
            command.DeviceId,
            command.IdentityId,
            command.Fingerprint);

        registry.Register(device);

        return new DeviceRegisteredEvent(
            command.IdentityId,
            command.DeviceId,
            device.RegisteredAt);
    }
}
```

---

# DEVICE TRUST ENGINE

Create:

```
DeviceTrustEngine.cs
```

```csharp
public sealed class DeviceTrustEngine
{
    public DeviceTrustedEvent Execute(
        TrustDeviceCommand command,
        DeviceRegistry registry)
    {
        var device = registry.Get(command.DeviceId);

        device.Trust();

        return new DeviceTrustedEvent(
            command.DeviceId,
            DateTime.UtcNow);
    }
}
```

---

# DEVICE REVOKE ENGINE

Create:

```
DeviceRevokeEngine.cs
```

```csharp
public sealed class DeviceRevokeEngine
{
    public DeviceRevokedEvent Execute(
        RevokeDeviceCommand command,
        DeviceRegistry registry)
    {
        var device = registry.Get(command.DeviceId);

        device.Revoke();

        return new DeviceRevokedEvent(
            command.DeviceId,
            DateTime.UtcNow);
    }
}
```

---

# UNIT TESTS

Create:

```
tests/Whycespace.DeviceTrust.Tests/
```

Tests:

```
DeviceRegistrationTests
DeviceTrustTests
DeviceRevocationTests
```

Example:

```csharp
[Fact]
public void DeviceRegistration_ShouldRegisterDevice()
{
    var registry = new DeviceRegistry();

    var engine = new DeviceRegisterEngine();

    var id = Guid.NewGuid();

    var result = engine.Execute(
        new RegisterDeviceCommand(id, "device-1", "fingerprint"),
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

Devices can be registered  
Devices can be trusted  
Devices can be revoked  
Device registry prevents duplicates  
Unit tests pass  

---

# END OF PHASE 2.0.7