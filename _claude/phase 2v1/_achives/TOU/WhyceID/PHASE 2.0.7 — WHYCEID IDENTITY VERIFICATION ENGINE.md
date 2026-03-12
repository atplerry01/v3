# WHYCESPACE WBSM v3
# PHASE 2.0.7 — WHYCEID IDENTITY VERIFICATION ENGINE

You are implementing **Phase 2.0.7 of the WhyceID System**.

This phase introduces the **Identity Verification Engine**, which manages
identity verification workflows.

Verification confirms that an identity is trusted within the system.

Examples:

user email verification
kyc verification
institution verification
service identity validation

Verification transitions the identity lifecycle from:

Pending → Verified

The engine must remain **stateless**.

Identity lifecycle enforcement is handled by the **IdentityAggregate**.

---

# ARCHITECTURE RULES

Follow WBSM v3 doctrine.

ENGINE
stateless orchestration

SYSTEM
stateful storage

The engine must:

• verify identity existence
• validate current identity state
• invoke aggregate verification
• update the registry

The engine must NOT persist data directly.

---

# TARGET LOCATION

Engine:

src/engines/T0U/WhyceID/

Create:

IdentityVerificationEngine.cs

This engine must interact with:

IdentityRegistry
IdentityAggregate

---

# VERIFICATION ENGINE

Create:

src/engines/T0U/WhyceID/IdentityVerificationEngine.cs

Purpose:

• verify identities
• enforce lifecycle rules
• delegate state transition to aggregate

Implementation:

using Whycespace.System.Upstream.WhyceID.Registry;
using Whycespace.System.Upstream.WhyceID.Aggregates;

namespace Whycespace.Engines.T0U.WhyceID;

public sealed class IdentityVerificationEngine
{
    private readonly IdentityRegistry _registry;

    public IdentityVerificationEngine(
        IdentityRegistry registry)
    {
        _registry = registry;
    }

    public void VerifyIdentity(Guid identityId)
    {
        if (!_registry.Exists(identityId))
        {
            throw new InvalidOperationException(
                $"Identity does not exist: {identityId}"
            );
        }

        var identity = _registry.Get(identityId);

        identity.Verify();

        _registry.Update(identity);
    }
}

---

# VERIFICATION RULES

Verification must enforce:

identity must exist
identity must be Pending
verified identity cannot be re-verified
revoked identity cannot be verified

These invariants are enforced by:

IdentityAggregate.Verify()

---

# TESTING

Create tests:

tests/engines/whyceid/

IdentityVerificationEngineTests.cs

Test scenarios:

identity verified successfully
missing identity rejected
verified identity cannot be reverified
revoked identity cannot be verified
registry update persists verification
verification timestamp set correctly

---

# DEBUG ENDPOINT

Add debug endpoint:

POST /dev/identity/{id}/verify

Purpose:

trigger verification during testing

Returns:

identity id
status
verified timestamp

Only available in DEBUG mode.

---

# SUCCESS CRITERIA

Build must succeed.

Requirements:

0 warnings
0 errors
all tests passing

Engine must remain stateless.

---

# NEXT PHASE

After this phase implement:

2.0.8 TrustScore Engine

This will introduce identity trust scoring based on:

verification level
device trust
behavior signals
policy compliance