```markdown
# WHYCESPACE WBSM v3
# PHASE 1.13 — CLUSTER ARCHITECTURE IMPLEMENTATION

You are implementing **Phase 1.13 of the Whycespace system**.

This phase implements the **Cluster Architecture Domain**.

Clusters represent **economic sectors**.

Each cluster contains:

ClusterAdministration  
ClusterProviders  
SubClusters  

SubClusters host:

SPVs

---

# OBJECTIVES

1 Implement Cluster Domain Model
2 Implement Cluster Administration Service
3 Implement Cluster Provider Service
4 Implement SubCluster Model
5 Integrate SPV registry
6 Bootstrap pilot clusters
7 Implement unit tests
8 Provide debug endpoints

---

# LOCATION

Create domain module:

src/domain/clusters/

Structure:

src/domain/clusters/
├── cluster/
├── administration/
├── providers/
├── subclusters/
└── registry/

Create project:

Whycespace.ClusterDomain.csproj

Target framework:

net8.0

References:

Whycespace.Contracts

---

# CLUSTER MODEL

Folder:

cluster/

Create:

Cluster.cs

Fields:

ClusterId  
ClusterName  
ClusterAuthorities  
SubClusters

Example:

public sealed class Cluster
{
    public Guid ClusterId { get; }

    public string ClusterName { get; }

    public IReadOnlyCollection<SubCluster> SubClusters { get; }

    public Cluster(Guid clusterId, string clusterName)
    {
        ClusterId = clusterId;
        ClusterName = clusterName;
        SubClusters = new List<SubCluster>();
    }
}

---

# SUBCLUSTER MODEL

Folder:

subclusters/

Create:

SubCluster.cs

Fields:

SubClusterId  
SubClusterName  
ParentClusterId

Example:

Taxi  
LettingAgent  

---

# CLUSTER ADMINISTRATION

Folder:

administration/

Create:

ClusterAdministrationService.cs

Responsibilities:

• register clusters  
• manage cluster configuration  
• manage subclusters  

Example methods:

RegisterCluster  
AddSubCluster  
GetCluster  

---

# CLUSTER PROVIDERS

Folder:

providers/

Create:

ClusterProvider.cs

Fields:

ProviderId  
ProviderName  
ClusterId  

Example providers:

DriverProvider  
VehicleProvider  
PropertyManager  
MaintenanceProvider  

---

Create:

ClusterProviderRegistry.cs

Purpose:

Register providers within cluster.

Methods:

RegisterProvider  
GetProviders  

---

# SPV REGISTRY

Folder:

registry/

Create:

SpvRegistry.cs

Purpose:

Track SPVs operating under subclusters.

Example:

public sealed class SpvRegistry
{
    private readonly Dictionary<Guid, string> _spvs;

    public void Register(Guid spvId, string subCluster)
}

---

# BOOTSTRAP PILOT CLUSTERS

Create bootstrap class:

ClusterBootstrapper.cs

Create clusters:

WhyceMobility  
WhyceProperty  

Create subclusters:

Taxi  
LettingAgent  

Example:

WhyceMobility
   └ Taxi

WhyceProperty
   └ LettingAgent

---

# SAMPLE REGISTRATION

Example code:

ClusterBootstrapper.RegisterCluster("WhyceMobility")

ClusterBootstrapper.AddSubCluster("Taxi")

ClusterBootstrapper.RegisterCluster("WhyceProperty")

ClusterBootstrapper.AddSubCluster("LettingAgent")

---

# INTEGRATE WITH EXECUTION ENGINES

Modify engines:

ClusterCreationEngine  
ClusterProviderRegistrationEngine  
SpvCreationEngine  

These engines must interact with:

ClusterAdministrationService  
ClusterProviderRegistry  
SpvRegistry  

---

# UNIT TESTS

Create project:

tests/cluster-domain/

Tests:

ClusterModelTests.cs  
ClusterAdministrationTests.cs  
ProviderRegistryTests.cs  
SpvRegistryTests.cs  

Test cases:

Create cluster  
Add subcluster  
Register provider  
Register SPV  

---

# DEBUG ENDPOINTS

Add endpoints.

GET /dev/clusters

Return:

{
  "clusters": [
    "WhyceMobility",
    "WhyceProperty"
  ]
}

---

GET /dev/clusters/subclusters

Return:

{
  "WhyceMobility": ["Taxi"],
  "WhyceProperty": ["LettingAgent"]
}

---

GET /dev/clusters/providers

Return provider registry.

---

# BUILD VALIDATION

Run:

dotnet build

Expected:

Build succeeded
0 warnings
0 errors

---

# TEST VALIDATION

Run:

dotnet test

Expected:

Tests:
4 passed
0 failed

---

# OUTPUT FORMAT

Return:

1 Files Created
2 Repository Tree
3 Build Result
4 Test Result
5 Debug Endpoints

Example:

Build succeeded
0 warnings
0 errors

Tests:
4 passed
0 failed

---

# PHASE COMPLETION CRITERIA

Phase 1.13 is complete when:

• cluster domain compiles
• pilot clusters bootstrap
• providers register
• SPVs register
• tests pass
• debug endpoints respond

End of Phase 1.13.
```
