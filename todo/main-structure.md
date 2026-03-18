

src/domain/

├── economic/
│   ├── capital/
│   ├── vault/
│   ├── asset/
│   ├── revenue/
│   ├── distribution/
│   ├── settlement/
│   ├── treasury/
│   ├── accounting/
│   ├── spv/
│   └── cluster/

├── identity/
├── governance/
├── workflow/
├── runtime/
└── shared/


src/infrastructure/

├── persistence/
│   ├── postgres/
│   ├── redis/
│   ├── eventstore/
│   └── projections/

├── messaging/
│   ├── kafka/
│   └── outbox/

├── policy/
│   └── opa/

├── identity/
│   └── providers/

└── configuration/



src/

├── domain/                     # Pure domain models
│   ├── economic/
│   ├── identity/
│   ├── governance/
│   ├── workflow/
│   └── shared/

├── engines/                    # Execution engines (T0–T4)
│   ├── T0U/
│   ├── T1M/
│   ├── T2E/
│   ├── T3I/
│   └── T4A/

├── systems/                    # Business orchestration
│   ├── upstream/
│   ├── midstream/
│   └── downstream/

├── runtime/                    # Execution platform
│   ├── dispatcher/
│   ├── event-fabric/
│   ├── projection/
│   ├── simulation/
│   └── observability/

├── infrastructure/             # External integrations
│   ├── persistence/
│   ├── messaging/
│   ├── policy/
│   └── identity/

├── platform/                   # UI / API gateway
├── shared/                     # Cross-layer contracts
s



===========================

src/domain/

├── economic/                  # Financial + asset logic
│   ├── capital/
│   ├── vault/
│   ├── asset/
│   ├── revenue/
│   ├── distribution/
│   ├── settlement/
│   ├── treasury/
│   ├── accounting/
│   ├── spv/
│   └── shared/
│
├── clusters/                  # WHYCE CLUSTER SYSTEM (CRITICAL)
│   ├── cluster/
│   ├── authority/
│   ├── subcluster/
│   ├── classification/
│   ├── lifecycle/
│   └── registry/
│
├── identity/                  # WhyceID domain
│   ├── identity/
│   ├── roles/
│   ├── permissions/
│   ├── trust/
│   ├── verification/
│   ├── consent/
│   └── session/
│
├── governance/                # WhyceGovernance domain
│   ├── proposal/
│   ├── voting/
│   ├── quorum/
│   ├── delegation/
│   ├── dispute/
│   ├── emergency/
│   ├── roles/
│   └── evidence/
│
├── workflow/                  # WSS domain model
│   ├── definition/
│   ├── graph/
│   ├── instance/
│   ├── lifecycle/
│   ├── dependency/
│   ├── versioning/
│   └── validation/
│
├── runtime/                   # Domain-level runtime concepts ONLY
│   ├── command/
│   ├── event/
│   ├── execution/
│   └── state/
│
└── shared/                    # Cross-domain primitives
    ├── value-objects/
    ├── identifiers/
    ├── money/
    ├── time/
    ├── enums/
    └── errors/


#####

src/domain/clusters/

├── governance/                     # LEGAL + CONTROL LAYER
│   ├── provider/                  # Who operates clusters (Whycespace / partners)
│   ├── administration/            # Admin bodies (cluster admins)
│   ├── authority/                 # Authority definitions (PropertyAuthority, etc.)
│   ├── subcluster/                # SubCluster definitions
│   ├── spv/                       # Legal/economic SPVs
│   ├── registry/                  # Cluster + SPV registry
│   └── lifecycle/                 # Legal lifecycle (formation → dissolution)
│
├── operations/                    # EXECUTION / GROWTH LAYER
│   ├── property/
│   ├── mobility/
│   ├── energy/
│   ├── finance/
│   └── shared/
│
├── classification/                # Cross-cutting classification
│   ├── cluster-type/
│   ├── authority-type/
│   └── risk-tier/
│
└── shared/
    ├── value-objects/
    ├── identifiers/
    └── events/