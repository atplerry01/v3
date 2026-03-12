# WHYCESPACE WBSM v3
# PHASE 1.11 — CORE EXECUTION ENGINES (T2E)

You are implementing **Phase 1.11 of the Whycespace system**.

This phase introduces the **Core Execution Engines**.

Execution engines implement domain logic and are invoked by workflows.

Engines must follow strict rules:

• stateless  
• deterministic  
• idempotent  
• no engine-to-engine calls  

All state mutation must emit events.

---

# OBJECTIVES

1 Implement Cluster Administration Engines  
2 Implement Cluster Provider Engines  
3 Implement SPV Lifecycle Engines  
4 Implement Vault Creation Engine  
5 Register engines in EngineRegistry  
6 Implement unit tests  
7 Provide debug endpoints  

---

# LOCATION

Create engines under:

```
src/engines/T2E_Execution/
```

Structure:

```
src/engines/T2E_Execution/

├── cluster/
├── providers/
├── spv/
└── economic/
```

---

# DOMAIN EVENTS LOCATION

Domain events MUST NOT live inside engines.

Create events under:

```
src/domain/

├── cluster/events/
├── providers/events/
├── spv/events/
└── economic/events/
```

---

# CLUSTER CREATION ENGINE

Folder:

```
src/engines/T2E_Execution/cluster/
```

Create:

```
ClusterCreationEngine.cs
```

Example:

```csharp
public sealed class ClusterCreationEngine : IEngine
{
    public string Name => "ClusterCreationEngine";

    public Task<EngineResult> ExecuteAsync(
        EngineContext context,
        CancellationToken cancellationToken)
    {
        var clusterEvent = new ClusterCreatedEvent(
            Guid.NewGuid(),
            context.WorkflowId,
            context.PartitionKey
        );

        return Task.FromResult(
            new EngineResult(
                true,
                new[] { clusterEvent },
                null
            )
        );
    }
}
```

---

# CLUSTER PROVIDER ENGINE

Folder:

```
src/engines/T2E_Execution/providers/
```

Create:

```
ClusterProviderRegistrationEngine.cs
```

Purpose:

Register a provider within a cluster.

Example providers:

```
DriverProvider
PropertyManager
```

Engine must emit:

```
ProviderRegisteredEvent
```

---

# SPV CREATION ENGINE

Folder:

```
src/engines/T2E_Execution/spv/
```

Create:

```
SpvCreationEngine.cs
```

Example:

```csharp
public sealed class SpvCreationEngine : IEngine
{
    public string Name => "SpvCreationEngine";

    public Task<EngineResult> ExecuteAsync(
        EngineContext context,
        CancellationToken cancellationToken)
    {
        var eventMessage = new SpvCreatedEvent(
            Guid.NewGuid(),
            context.WorkflowId,
            context.PartitionKey
        );

        return Task.FromResult(
            new EngineResult(
                true,
                new[] { eventMessage },
                null
            )
        );
    }
}
```

---

# VAULT CREATION ENGINE

Folder:

```
src/engines/T2E_Execution/economic/
```

Create:

```
VaultCreationEngine.cs
```

Purpose:

Create a participant vault.

Example:

```csharp
public sealed class VaultCreationEngine : IEngine
{
    public string Name => "VaultCreationEngine";

    public Task<EngineResult> ExecuteAsync(
        EngineContext context,
        CancellationToken cancellationToken)
    {
        var vaultEvent = new VaultCreatedEvent(
            Guid.NewGuid(),
            context.WorkflowId,
            context.PartitionKey
        );

        return Task.FromResult(
            new EngineResult(
                true,
                new[] { vaultEvent },
                null
            )
        );
    }
}
```

---

# ENGINE REGISTRATION

Update:

```
EngineBootstrapper
```

Register engines:

```
ClusterCreationEngine
ClusterProviderRegistrationEngine
SpvCreationEngine
VaultCreationEngine
```

---

# SAMPLE WORKFLOW INTEGRATION

Example workflow:

```
ClusterBootstrapWorkflow
```

Steps:

```
ClusterCreationEngine
ClusterProviderRegistrationEngine
SpvCreationEngine
VaultCreationEngine
```

---

# UNIT TESTS

Create project:

```
tests/execution-engines/
```

Tests:

```
ClusterCreationEngineTests.cs
ProviderRegistrationEngineTests.cs
SpvCreationEngineTests.cs
VaultCreationEngineTests.cs
```

Test cases:

```
engine execution
event generation
engine idempotency
```

---

# DEBUG ENDPOINTS

Add endpoints.

GET

```
/dev/engines/t2e
```

Return registered execution engines.

Example:

```json
{
  "executionEngines": [
    "ClusterCreationEngine",
    "ClusterProviderRegistrationEngine",
    "SpvCreationEngine",
    "VaultCreationEngine"
  ]
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
0 warnings
0 errors
```

---

# TEST VALIDATION

Run:

```
dotnet test
```

Expected:

```
Tests:
4 passed
0 failed
```

---

# OUTPUT FORMAT

Return:

```
1 Files Created
2 Repository Tree
3 Build Result
4 Test Result
5 Debug Endpoints
```

Example:

```
Build succeeded
0 warnings
0 errors

Tests:
4 passed
0 failed
```

---

# PHASE COMPLETION CRITERIA

Phase 1.11 is complete when:

• execution engines compile  
• engines generate domain events  
• engines are registered  
• tests pass  
• debug endpoints respond  

End of Phase 1.11.