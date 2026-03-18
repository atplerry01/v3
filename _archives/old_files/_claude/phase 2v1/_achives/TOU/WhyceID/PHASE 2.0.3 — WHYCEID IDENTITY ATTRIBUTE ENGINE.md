# WHYCESPACE WBSM v3
# PHASE 2.0.3 — WHYCEID IDENTITY ATTRIBUTE ENGINE

You are implementing **Phase 2.0.3 of the WhyceID System**.

This phase introduces the **Identity Attribute Engine**, which manages
attributes attached to identities.

Attributes represent **identity metadata** used across the platform.

Examples:

email
phone
country
kyc_level
organization
risk_level
verification_tier
device_fingerprint

Attributes enable:

RBAC
ABAC
TrustScore
Identity verification
Policy enforcement

This engine must remain **stateless**.

Attributes are stored in the **Identity Attribute Store** inside the WhyceID system layer.

---

# ARCHITECTURE RULES

Follow WBSM v3 doctrine.

ENGINE
stateless business logic

SYSTEM
stateful storage

The engine must:

• validate attribute mutations
• ensure identity exists
• produce deterministic results
• interact with IdentityRegistry

The engine must NOT persist data directly.

Persistence will be handled through:

IdentityAttributeStore

---

# TARGET LOCATION

Engine location:

src/engines/T0U/WhyceID/

Create:

IdentityAttributeEngine.cs

System store location:

src/system/upstream/WhyceID/stores/

Create:

IdentityAttributeStore.cs

---

# OBJECTIVES

1 Implement IdentityAttributeEngine
2 Implement IdentityAttributeStore
3 Support attribute creation
4 Support attribute updates
5 Support attribute retrieval
6 Validate attribute keys
7 Prevent null attribute values
8 Ensure identity exists before mutation

---

# ATTRIBUTE MODEL

Create model:

src/system/upstream/WhyceID/models/IdentityAttribute.cs

Implementation:

namespace Whycespace.System.Upstream.WhyceID.Models;

public sealed record IdentityAttribute
(
    string Key,
    string Value,
    DateTime CreatedAt
);

Validation rules:

Key cannot be null or empty  
Value cannot be null  

---

# ATTRIBUTE STORE

Create:

stores/IdentityAttributeStore.cs

Purpose:

• store attributes per identity
• allow attribute lookup
• thread-safe storage

Implementation:

using System.Collections.Concurrent;
using Whycespace.System.Upstream.WhyceID.Models;

namespace Whycespace.System.Upstream.WhyceID.Stores;

public sealed class IdentityAttributeStore
{
    private readonly ConcurrentDictionary<Guid, List<IdentityAttribute>> _store = new();

    public void Add(Guid identityId, IdentityAttribute attribute)
    {
        var attributes = _store.GetOrAdd(identityId, _ => new List<IdentityAttribute>());

        attributes.Add(attribute);
    }

    public IReadOnlyList<IdentityAttribute> Get(Guid identityId)
    {
        if (_store.TryGetValue(identityId, out var attributes))
        {
            return attributes;
        }

        return Array.Empty<IdentityAttribute>();
    }
}

---

# ATTRIBUTE ENGINE

Create:

src/engines/T0U/WhyceID/IdentityAttributeEngine.cs

Purpose:

• validate attribute mutation
• enforce attribute rules
• interact with registry

Implementation:

using Whycespace.System.Upstream.WhyceID.Registry;
using Whycespace.System.Upstream.WhyceID.Stores;
using Whycespace.System.Upstream.WhyceID.Models;

namespace Whycespace.Engines.T0U.WhyceID;

public sealed class IdentityAttributeEngine
{
    private readonly IdentityRegistry _registry;
    private readonly IdentityAttributeStore _store;

    public IdentityAttributeEngine(
        IdentityRegistry registry,
        IdentityAttributeStore store)
    {
        _registry = registry;
        _store = store;
    }

    public void AddAttribute(Guid identityId, string key, string value)
    {
        if (!_registry.Exists(identityId))
        {
            throw new InvalidOperationException(
                $"Identity does not exist: {identityId}"
            );
        }

        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("Attribute key cannot be empty.");
        }

        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Attribute value cannot be empty.");
        }

        var attribute = new IdentityAttribute(
            key,
            value,
            DateTime.UtcNow
        );

        _store.Add(identityId, attribute);
    }

    public IReadOnlyList<IdentityAttribute> GetAttributes(Guid identityId)
    {
        return _store.Get(identityId);
    }
}

---

# TESTING

Create tests:

tests/engines/whyceid/

IdentityAttributeEngineTests.cs

Test scenarios:

attribute added successfully  
attribute rejected when identity missing  
empty key rejected  
empty value rejected  
attributes retrievable  
multiple attributes supported  

---

# DEBUG ENDPOINT

Add debug endpoint:

GET /dev/identity/{id}/attributes

Returns:

identity id  
list of attributes  

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

2.0.4 Identity Role Engine

This will introduce RBAC capability on top of attributes.s