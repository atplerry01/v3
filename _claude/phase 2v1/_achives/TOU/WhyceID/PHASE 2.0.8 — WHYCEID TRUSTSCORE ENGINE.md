# WHYCESPACE WBSM v3
# PHASE 2.0.8 — WHYCEID TRUSTSCORE ENGINE

You are implementing **Phase 2.0.8 of the WhyceID System**.

This phase introduces the **TrustScore Engine**, which calculates
a dynamic trust score for identities.

TrustScore represents the **confidence level** that the system has
in an identity.

The score will later be used by:

Authentication Engine
Authorization Engine
Policy Enforcement

TrustScore is calculated from signals such as:

identity verification status
identity age
device trust
behavior signals
policy compliance

For Phase 2.0.8 we implement a **foundational trust model**.

The engine must remain **stateless**.

TrustScore persistence is handled by the **IdentityTrustStore**.

---

# ARCHITECTURE RULES

Follow WBSM v3 doctrine.

ENGINE
stateless logic

SYSTEM
stateful storage

The engine must:

• calculate trust score
• retrieve existing trust score
• update trust score
• validate identity existence

The engine must NOT persist data directly.

---

# TARGET LOCATIONS

Engine:

src/engines/T0U/WhyceID/

Create:

TrustScoreEngine.cs

System Store:

src/system/upstream/WhyceID/stores/

Create:

IdentityTrustStore.cs

Model:

src/system/upstream/WhyceID/models/

Create:

IdentityTrustScore.cs

---

# TRUST SCORE MODEL

Create:

models/IdentityTrustScore.cs

Implementation:

namespace Whycespace.System.Upstream.WhyceID.Models;

public sealed record IdentityTrustScore
(
    int Score,
    DateTime CalculatedAt
);

Validation rules:

Score range must be:

0 to 100

Meaning:

0  = untrusted
100 = fully trusted

---

# TRUST STORE

Create:

stores/IdentityTrustStore.cs

Purpose:

• store trust scores for identities
• retrieve trust score
• thread-safe storage

Implementation:

using System.Collections.Concurrent;
using Whycespace.System.Upstream.WhyceID.Models;

namespace Whycespace.System.Upstream.WhyceID.Stores;

public sealed class IdentityTrustStore
{
    private readonly ConcurrentDictionary<Guid, IdentityTrustScore> _scores = new();

    public void Update(Guid identityId, IdentityTrustScore score)
    {
        _scores[identityId] = score;
    }

    public IdentityTrustScore? Get(Guid identityId)
    {
        if (_scores.TryGetValue(identityId, out var score))
        {
            return score;
        }

        return null;
    }
}

---

# TRUST SCORE ENGINE

Create:

src/engines/T0U/WhyceID/TrustScoreEngine.cs

Purpose:

• calculate identity trust score
• enforce score range
• persist via store

Implementation:

using Whycespace.System.Upstream.WhyceID.Registry;
using Whycespace.System.Upstream.WhyceID.Models;
using Whycespace.System.Upstream.WhyceID.Stores;

namespace Whycespace.Engines.T0U.WhyceID;

public sealed class TrustScoreEngine
{
    private readonly IdentityRegistry _registry;
    private readonly IdentityTrustStore _store;

    public TrustScoreEngine(
        IdentityRegistry registry,
        IdentityTrustStore store)
    {
        _registry = registry;
        _store = store;
    }

    public IdentityTrustScore Calculate(Guid identityId)
    {
        if (!_registry.Exists(identityId))
        {
            throw new InvalidOperationException(
                $"Identity does not exist: {identityId}"
            );
        }

        var identity = _registry.Get(identityId);

        int score = 0;

        if (identity.Status == IdentityStatus.Verified)
        {
            score += 50;
        }

        var age = DateTime.UtcNow - identity.CreatedAt;

        if (age.TotalDays > 30)
        {
            score += 25;
        }

        if (age.TotalDays > 180)
        {
            score += 25;
        }

        var trust = new IdentityTrustScore(
            score,
            DateTime.UtcNow
        );

        _store.Update(identityId, trust);

        return trust;
    }

    public IdentityTrustScore? Get(Guid identityId)
    {
        return _store.Get(identityId);
    }
}

---

# TRUST SCORE TIERS

TrustScore ranges:

0–20     Untrusted
21–50    Low Trust
51–75    Medium Trust
76–100   High Trust

Later phases will extend the score using:

device trust
session behavior
policy compliance
guardian overrides

---

# TESTING

Create tests:

tests/engines/whyceid/

TrustScoreEngineTests.cs

Test scenarios:

trust score calculated correctly
missing identity rejected
verified identity increases score
identity age increases score
trust score stored correctly
Get returns stored score

---

# DEBUG ENDPOINT

Add debug endpoint:

POST /dev/identity/{id}/trustscore

Returns:

identity id
trust score
calculation timestamp

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

2.0.9 Device Trust Engine

This will introduce trusted devices and
device-level trust scoring for authentication.