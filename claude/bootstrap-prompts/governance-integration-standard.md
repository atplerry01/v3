# WHYCESPACE WBSM v3 — GOVERNANCE INTEGRATION STANDARD

Status: LOCKED
Version: WBSM v3
Scope: WhycePolicy, WhyceID, WhyceChain Integration
Companions: [architecture-lock.md](architecture-lock.md), [runtime-execution-model.md](runtime-execution-model.md), [workflow-system-standard.md](workflow-system-standard.md)

---

## 1. GOVERNANCE SYSTEMS

Whycespace governance consists of three upstream systems:

| System      | Tier | Responsibility                |
|-------------|------|-------------------------------|
| WhyceID     | T0U  | Identity & authorization      |
| WhycePolicy | T0U  | Policy-as-code enforcement    |
| WhyceChain  | T0U  | Evidence integrity anchoring  |

Governance is **orthogonal to business logic** — engines must never implement authorization or policy logic.

---

## 2. GOVERNANCE EXECUTION PIPELINE

Every command passes through governance before reaching engine execution:

```
Command
  -> Identity Validation (WhyceID)
  -> Authorization (WhycePolicy)
  -> Workflow Creation
  -> Engine Execution
  -> Event Emission
  -> Evidence Anchoring (WhyceChain)
```

Governance failures **prevent workflow execution** — they are not advisory.

---

## 3. IDENTITY ENFORCEMENT (WhyceID)

WhyceID validates identity before any command is processed:

| Validation          | Purpose                        |
|---------------------|--------------------------------|
| Identity verification | Confirm actor identity       |
| Role validation      | Verify actor role permissions |
| Service identity     | Validate service-to-service auth |

### IdentityContext Propagation

`IdentityContext` is created at the access layer and propagated through the entire execution pipeline:

```
Access Layer -> IdentityContext -> Workflow -> Engine -> Event Metadata
```

IdentityContext fields:

| Field          | Purpose                    |
|----------------|----------------------------|
| IdentityId     | Verified actor identifier  |
| Roles          | Actor role set             |
| ServiceId      | Originating service        |
| SessionId      | Session tracking           |

---

## 4. POLICY EVALUATION (WhycePolicy)

WhycePolicy evaluates authorization after identity validation:

```
PolicyInput -> PolicyEvaluation -> PolicyDecision
```

| Component        | Purpose                          |
|------------------|----------------------------------|
| PolicyInput      | Command + IdentityContext + resource |
| PolicyEvaluation | Rule engine execution            |
| PolicyDecision   | Allow / Deny + reason            |

Policy decisions must **always emit events** — both approvals and denials are recorded.

---

## 5. GOVERNANCE EVENT TYPES

| Event                      | Trigger                        |
|----------------------------|--------------------------------|
| IdentityVerified           | Successful identity validation |
| IdentityRejected           | Failed identity validation     |
| PolicyDecisionEvaluated    | Policy rule evaluated          |
| PolicyViolationDetected    | Authorization denied           |
| GovernanceAuditRecorded    | Audit checkpoint emitted       |

Events must follow [event-fabric-kafka-standard.md](event-fabric-kafka-standard.md).

Topic: `whyce.policy.events`, `whyce.identity.events`

---

## 6. EVIDENCE ANCHORING (WhyceChain)

Critical governance and economic events anchor to WhyceChain for tamper-proof integrity:

```
Event -> EvidenceRecorder -> MerkleProofBuilder -> WhyceChainAnchor
```

Events requiring anchoring:

| Event                        | Domain              |
|------------------------------|---------------------|
| PolicyDecisionEvaluated      | Governance          |
| CapitalContributionRecorded  | Capital             |
| RevenueRecorded              | Revenue             |
| ProfitDistributed            | Profit distribution |

WhyceChain anchoring is **post-execution** — it does not block the runtime pipeline.

---

## 7. GOVERNANCE FAILURE HANDLING

Governance failures block execution — they are not recoverable via retry:

| Failure               | Effect                          | Recovery                |
|-----------------------|---------------------------------|-------------------------|
| IdentityFailure       | Command rejected                | Re-authenticate         |
| AuthorizationFailure  | Workflow not created            | Request policy change   |
| PolicyViolation       | Engine execution prevented      | Correct input or policy |

Governance failures must:

- Emit failure events
- Be observable in metrics
- Never be silently swallowed

---

## 8. GOVERNANCE OBSERVABILITY

| Metric                | Purpose                       |
|-----------------------|-------------------------------|
| `policy_decisions`    | Total policy evaluations      |
| `policy_denials`      | Authorization denial count    |
| `identity_failures`   | Identity validation failures  |
| `governance_latency`  | End-to-end governance time    |

For full observability layer, see [architecture-lock.md](architecture-lock.md) section 15.
