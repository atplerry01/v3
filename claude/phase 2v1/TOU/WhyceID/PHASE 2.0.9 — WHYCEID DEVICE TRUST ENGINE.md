# WHYCESPACE WBSM v3
# PHASE 2.0.9 — WHYCEID DEVICE TRUST ENGINE

You are implementing **Phase 2.0.9 of the WhyceID System**.

This phase introduces the **Device Trust Engine**, which manages trusted devices
associated with identities.

Device trust ensures that system access is not granted solely based on identity,
but also on whether the device being used is trusted.

Examples of devices:

laptop
mobile phone
tablet
server
service runtime

Each device will have:

device id
device fingerprint
trust level
registration timestamp

The engine must remain **stateless**.

Device persistence is handled by the **IdentityDeviceStore**.

---

# ARCHITECTURE RULES

Follow WBSM v3 doctrine.

ENGINE
stateless logic

SYSTEM
stateful storage

The engine must:

• register devices
• validate identity existence
• mark device as trusted
• retrieve device trust status
• revoke device trust

The engine must NOT persist data directly.

---

# TARGET LOCATIONS

Engine:

src/engines/T0U/WhyceID/

Create:

DeviceTrustEngine.cs

System Store:

src/system/upstream/WhyceID/stores/

Create:

IdentityDeviceStore.cs

Model:

src/system/upstream/WhyceID/models/

Create:

IdentityDevice.cs

---

# DEVICE MODEL

Create:

models/IdentityDevice.cs

Implementation:

namespace Whycespace.System.Upstream.WhyceID.Models;

public sealed record IdentityDevice
(
    Guid DeviceId,
    string Fingerprint,
    bool Trusted,
    DateTime RegisteredAt
);

Validation rules:

DeviceId must not be empty
Fingerprint must not be empty

Fingerprint examples:

device hash
browser fingerprint
hardware identifier
service runtime identifier

---

# DEVICE STORE

Create:

stores/IdentityDeviceStore.cs

Purpose:

• store identity devices
• retrieve devices for identity
• manage trusted device status
• thread-safe storage

Implementation:

using System.Collections.Concurrent;
using Whycespace.System.Upstream.WhyceID.Models;

namespace Whycespace.System.Upstream.WhyceID.Stores;

public sealed class IdentityDeviceStore
{
    private readonly ConcurrentDictionary<Guid, List<IdentityDevice>> _devices = new();

    public void Register(Guid identityId, IdentityDevice device)
    {
        var list = _devices.GetOrAdd(identityId, _ => new List<IdentityDevice>());

        lock (list)
        {
            list.Add(device);
        }
    }

    public IReadOnlyCollection<IdentityDevice> Get(Guid identityId)
    {
        if (_devices.TryGetValue(identityId, out var list))
        {
            return list.ToList();
        }

        return Array.Empty<IdentityDevice>();
    }

    public void TrustDevice(Guid identityId, Guid deviceId)
    {
        if (_devices.TryGetValue(identityId, out var list))
        {
            lock (list)
            {
                var device = list.FirstOrDefault(d => d.DeviceId == deviceId);

                if (device is not null)
                {
                    list.Remove(device);

                    list.Add(device with { Trusted = true });
                }
            }
        }
    }

    public bool IsTrusted(Guid identityId, Guid deviceId)
    {
        if (_devices.TryGetValue(identityId, out var list))
        {
            return list.Any(d => d.DeviceId == deviceId && d.Trusted);
        }

        return false;
    }
}

---

# DEVICE TRUST ENGINE

Create:

src/engines/T0U/WhyceID/DeviceTrustEngine.cs

Purpose:

• register devices
• mark device trusted
• retrieve device trust state
• revoke trust

Implementation:

using Whycespace.System.Upstream.WhyceID.Registry;
using Whycespace.System.Upstream.WhyceID.Models;
using Whycespace.System.Upstream.WhyceID.Stores;

namespace Whycespace.Engines.T0U.WhyceID;

public sealed class DeviceTrustEngine
{
    private readonly IdentityRegistry _registry;
    private readonly IdentityDeviceStore _store;

    public DeviceTrustEngine(
        IdentityRegistry registry,
        IdentityDeviceStore store)
    {
        _registry = registry;
        _store = store;
    }

    public void RegisterDevice(Guid identityId, string fingerprint)
    {
        if (!_registry.Exists(identityId))
        {
            throw new InvalidOperationException(
                $"Identity does not exist: {identityId}"
            );
        }

        if (string.IsNullOrWhiteSpace(fingerprint))
        {
            throw new ArgumentException("Device fingerprint cannot be empty.");
        }

        var device = new IdentityDevice(
            Guid.NewGuid(),
            fingerprint,
            false,
            DateTime.UtcNow
        );

        _store.Register(identityId, device);
    }

    public void TrustDevice(Guid identityId, Guid deviceId)
    {
        _store.TrustDevice(identityId, deviceId);
    }

    public bool IsTrusted(Guid identityId, Guid deviceId)
    {
        return _store.IsTrusted(identityId, deviceId);
    }

    public IReadOnlyCollection<IdentityDevice> GetDevices(Guid identityId)
    {
        return _store.Get(identityId);
    }
}

---

# TESTING

Create tests:

tests/engines/whyceid/

DeviceTrustEngineTests.cs

Test scenarios:

device registered successfully
missing identity rejected
device trusted successfully
trusted device recognized
device list retrieved
duplicate devices allowed
invalid fingerprint rejected

---

# DEBUG ENDPOINT

Add debug endpoint:

GET /dev/identity/{id}/devices

Returns:

identity id
registered devices
trusted devices

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

2.0.10 Authentication Engine

This will introduce login flows and session creation using:

identity
device trust
trust score
policy evaluation