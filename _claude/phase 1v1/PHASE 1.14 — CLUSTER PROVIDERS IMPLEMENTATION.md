# WHYCESPACE WBSM v3
# PHASE 1.14 — CLUSTER PROVIDERS IMPLEMENTATION

You are implementing **Phase 1.14 of the Whycespace system**.

This phase activates the **Cluster Providers Domain**, which represents the institutional supply layer of clusters.

Providers supply economic activity to SPVs operating under subclusters.

Example providers:

Mobility cluster  
• DriverProvider  
• VehicleProvider  

Property cluster  
• PropertyManagerProvider  
• MaintenanceProvider  

Providers must be:

• registered within a cluster  
• linked to subclusters  
• available to workflows and decision engines through projections  

---

# OBJECTIVES

1 Implement Provider Domain Model  
2 Implement Provider Registry  
3 Implement Provider Assignment to SubClusters  
4 Integrate Providers with Decision Engines through projections  
5 Bootstrap Pilot Providers  
6 Implement unit tests  
7 Provide debug endpoints  

---

# LOCATION

Providers belong to the **Cluster Domain**.

Create module:

```
src/domain/cluster/providers/
```

Structure:

```
src/domain/cluster/providers/
├── models/
├── registry/
└── bootstrap/
```

Provider assignment belongs to **Cluster Administration**.

```
src/domain/cluster/administration/
```

Create project:

```
Whycespace.ClusterDomain.csproj
```

Target framework:

```
net8.0
```

References:

```
Whycespace.Contracts
```

---

# PROVIDER MODEL

Folder:

```
models/
```

Create:

```
Provider.cs
```

Fields:

```
ProviderId
ProviderName
ProviderType
ClusterId
```

Example types:

```
DriverProvider
VehicleProvider
PropertyManagerProvider
MaintenanceProvider
```

Example class:

```csharp
public sealed class Provider
{
    public Guid ProviderId { get; }

    public string ProviderName { get; }

    public string ProviderType { get; }

    public Guid ClusterId { get; }

    public Provider(Guid providerId, string name, string type, Guid clusterId)
    {
        ProviderId = providerId;
        ProviderName = name;
        ProviderType = type;
        ClusterId = clusterId;
    }
}
```

---

# PROVIDER REGISTRY

Folder:

```
registry/
```

Create:

```
ProviderRegistry.cs
```

Purpose:

Store providers per cluster.

Example methods:

```
RegisterProvider
GetProvidersByCluster
GetProvider
```

Example storage:

```
Dictionary<Guid, Provider>
```

---

# PROVIDER ASSIGNMENT SERVICE

Location:

```
src/domain/cluster/administration/
```

Create:

```
ProviderAssignmentService.cs
```

Purpose:

Assign providers to subclusters.

Example assignments:

```
DriverProvider → Taxi
PropertyManagerProvider → LettingAgent
```

Example methods:

```
AssignProviderToSubCluster
GetProvidersForSubCluster
```

Example storage:

```
Dictionary<string, List<Guid>>
```

---

# BOOTSTRAP PILOT PROVIDERS

Folder:

```
bootstrap/
```

Create:

```
ProviderBootstrapper.cs
```

Bootstrap providers.

WhyceMobility cluster:

```
DriverProvider
VehicleProvider
```

WhyceProperty cluster:

```
PropertyManagerProvider
MaintenanceProvider
```

Example:

```
ProviderBootstrapper.RegisterProvider(
    "DriverProvider",
    "WhyceMobility"
)

ProviderBootstrapper.RegisterProvider(
    "PropertyManagerProvider",
    "WhyceProperty"
)
```

---

# PROVIDER USAGE IN DECISION ENGINES

Decision engines must **not query the domain directly**.

Correct flow:

```
ProviderRegistry (domain)
↓
ProviderProjection (runtime)
↓
ProjectionQueryService
↓
DecisionEngine
```

Example:

```
DriverMatchingEngine
TenantMatchingEngine
```

These engines query providers via:

```
ProjectionQueryService
```

---

# SAMPLE PROVIDER FLOW

Taxi ride request

```
RideRequestWorkflow
↓
DriverMatchingEngine
↓
Query DriverProvider projection
↓
Select available driver
```

---

Property letting

```
TenantApplicationWorkflow
↓
TenantMatchingEngine
↓
Query PropertyManagerProvider projection
↓
Assign property manager
```

---

# UNIT TESTS

Create project:

```
tests/provider-domain/
```

Tests:

```
ProviderModelTests.cs
ProviderRegistryTests.cs
ProviderAssignmentTests.cs
ProviderBootstrapTests.cs
```

Test cases:

```
create provider
register provider
assign provider to subcluster
query providers
```

---

# DEBUG ENDPOINTS

Add endpoints.

GET

```
/dev/providers
```

Return registered providers.

Example:

```json
{
  "providers": [
    "DriverProvider",
    "VehicleProvider",
    "PropertyManagerProvider",
    "MaintenanceProvider"
  ]
}
```

---

GET

```
/dev/providers/assignments
```

Return provider assignments.

Example:

```json
{
  "Taxi": ["DriverProvider", "VehicleProvider"],
  "LettingAgent": ["PropertyManagerProvider", "MaintenanceProvider"]
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

Phase 1.14 is complete when:

• provider domain compiles  
• providers bootstrap correctly  
• providers assign to subclusters  
• decision engines access providers through projections  
• tests pass  
• debug endpoints respond  

End of Phase 1.14.