# Whycespace WBSM v3 — Claude Code Configuration

## Canonical Architecture Documents

Before writing any code, read and enforce:

1. **[Architecture Lock](claude/bootstrap-prompts/architecture-lock.md)** — Locked runtime architecture specification (layers, event fabric, reliability, partitions, projections)
2. **[Runtime Execution Model](claude/bootstrap-prompts/runtime-execution-model.md)** — Workflow lifecycle, graph execution, state tracking, scaling, evidence anchoring
3. **[Implementation Guardrails](claude/bootstrap-prompts/implementation-guardrails.md)** — Layer rules, repository structure, code generation safety, validation checklist
4. **[Engine Implementation Standard](claude/bootstrap-prompts/engine-implementation-standard.md)** — C# engine contracts, file structure, input/result models, testing, versioning
5. **[Projection & Read Model Standard](claude/bootstrap-prompts/projection-read-model-standard.md)** — CQRS projections, stores, rebuild, query services, cluster projections
6. **[Event Fabric & Kafka Standard](claude/bootstrap-prompts/event-fabric-kafka-standard.md)** — Topic naming, producer/consumer rules, retention, exactly-once, event security
7. **[Workflow System Standard](claude/bootstrap-prompts/workflow-system-standard.md)** — WSS orchestration, saga/compensation, lifecycle events, workflow persistence
8. **[Cluster Runtime Standard](claude/bootstrap-prompts/cluster-runtime-standard.md)** — Cluster hierarchy, economic integration, registry, extensibility
9. **[Event Store & Persistence Standard](claude/bootstrap-prompts/event-store-persistence-standard.md)** — Event sourcing, aggregate reconstruction, snapshots, storage tiering, archival
10. **[Governance Integration Standard](claude/bootstrap-prompts/governance-integration-standard.md)** — WhycePolicy, WhyceID, WhyceChain pipeline, identity context, policy evaluation
11. **[Prompt Generation Standard](claude/bootstrap-prompts/prompt-generation-standard.md)** — AI prompt structure, naming, versioning, execution flow, templates

## Core Rules

- Follow strict layered architecture: `domain -> system -> engines -> runtime -> platform -> infrastructure`
- Engines are stateless, deterministic, thread-safe. They communicate via events only.
- Engine tiers: T0U (Constitutional), T1M (Orchestration), T2E (Execution), T3I (Intelligence), T4A (Access)
- Clusters are bounded contexts — no cross-cluster dependencies
- Events are the only inter-engine communication mechanism
- If a path or placement is unclear — STOP and ask before generating code
- No architectural deviations without constitutional amendment
