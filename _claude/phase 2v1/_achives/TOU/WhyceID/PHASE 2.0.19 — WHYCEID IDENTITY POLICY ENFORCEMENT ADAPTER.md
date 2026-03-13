# WHYCESPACE WBSM v3
# PHASE 2.0.19 — WHYCEID IDENTITY POLICY ENFORCEMENT ADAPTER

You are implementing **Phase 2.0.19 of the WhyceID System**.

This phase introduces the **Identity Policy Enforcement Adapter**, which
connects WhyceID with the WHYCEPOLICY engine.

The adapter ensures that identity-based actions are validated
against governance policies before execution.

Examples of policies enforced:

minimum trust score required
role-based access rules
cluster administrator restrictions
guardian governance requirements
revoked identity blocking

---

# ARCHITECTURE RULES

Follow WBSM v3 doctrine.

ENGINE  
stateless logic

SYSTEM  
stateful storage

Policy enforcement must delegate evaluation to the **PolicyEvaluationEngine**.

The adapter must NOT store state.

---

# ADAPTER CONCEPT

The adapter acts as a bridge between:

WhyceID  
WHYCEPOLICY

It gathers identity context and forwards it to the policy engine.

Identity context includes:

identity id  
roles  
permissions  
trust score  
verification status  
revocation status  

The adapter returns:

policy allow  
policy deny  
policy reason  

---

# TARGET LOCATION

Engine:

src/engines/T0U/WhyceID/

Create:

IdentityPolicyEnforcementAdapter.cs

Dependencies:

IdentityRegistry  
IdentityRoleStore  
IdentityPermissionStore  
IdentityTrustStore  
IdentityRevocationStore  
PolicyEvaluationEngine  

---

# POLICY REQUEST MODEL

Create:

models/IdentityPolicyContext.cs

Location:

src/system/upstream/WhyceID/models/

Implementation:

namespace Whycespace.System.Upstream.WhyceID.Models;

public sealed record IdentityPolicyContext(
    Guid IdentityId,
    IReadOnlyCollection<string> Roles,
    int TrustScore,
    bool Verified,
    bool Revoked
);

# POLICY ENFORCEMENT ADAPTER

Create:

src/engines/T0U/WhyceID/IdentityPolicyEnforcementAdapter.cs

Implementation:

using Whycespace.System.Upstream.WhyceID.Registry;
using Whycespace.System.Upstream.WhyceID.Stores;
using Whycespace.System.Upstream.WhyceID.Models;
using Whycespace.Engines.T0U.Constitutional;

namespace Whycespace.Engines.T0U.WhyceID;

public sealed class IdentityPolicyEnforcementAdapter
{
    private readonly IdentityRegistry _registry;
    private readonly IdentityRoleStore _roleStore;
    private readonly IdentityTrustStore _trustStore;
    private readonly IdentityRevocationStore _revocationStore;
    private readonly PolicyEvaluationEngine _policyEngine;

    public IdentityPolicyEnforcementAdapter(
        IdentityRegistry registry,
        IdentityRoleStore roleStore,
        IdentityTrustStore trustStore,
        IdentityRevocationStore revocationStore,
        PolicyEvaluationEngine policyEngine)
    {
        _registry = registry;
        _roleStore = roleStore;
        _trustStore = trustStore;
        _revocationStore = revocationStore;
        _policyEngine = policyEngine;
    }

    public bool EvaluateIdentityAccess(Guid identityId)
    {
        if (!_registry.Exists(identityId))
            throw new InvalidOperationException($"Identity does not exist: {identityId}");

        var roles = _roleStore.Get(identityId)
            .Select(r => r.RoleName)
            .ToList();

        var trust = _trustStore.Get(identityId).Score;

        var revoked = _revocationStore.IsRevoked(identityId);

        var identity = _registry.Get(identityId);

        var context = new IdentityPolicyContext(
            identityId,
            roles,
            trust,
            identity.Status == IdentityStatus.Verified,
            revoked
        );

        return _policyEngine.Evaluate(context);
    }
}

# TESTING

Create tests:

tests/engines/whyceid/

IdentityPolicyAdapterTests.cs

Test scenarios:

identity exists and policy allows
identity exists and policy denies
missing identity rejected
revoked identity blocked
low trust score blocked
verified identity allowed


# DEBUG ENDPOINT

Add debug endpoint:

POST /dev/identity/policy/evaluate

Input:

identityId

Returns:

allowed
denied

Include context used for evaluation.

Only available in DEBUG mode.

# SUCCESS CRITERIA

Build must succeed.

Requirements:

0 warnings  
0 errors  
all tests passing  

Adapter must remain stateless.

Policy evaluation must be delegated to WHYCEPOLICY engine.

# NEXT PHASE

After this phase implement:

2.0.20 Identity Audit Engine

The audit engine will record identity actions across the system,
including:

authentication attempts
authorization decisions
policy enforcement
role assignments
device registrations