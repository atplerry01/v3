# WHYCESPACE WBSM v3
# PHASE 1.14.5 — CLUSTER TEMPLATE GENERATOR

You are implementing **Phase 1.14.5 of the Whycespace system**.

This phase introduces the **Cluster Template Generator**.

Cluster templates allow the platform to automatically generate the full structure of a new cluster.

Each cluster must follow the canonical structure:

Cluster  
├── ClusterAdministration  
├── ClusterProviders  
└── SubClusters  
      ↓  
      SPVs  

The generator ensures every cluster follows this structure automatically.

---

# OBJECTIVES

1 Implement Cluster Template Model  
2 Implement Template Registry  
3 Implement Cluster Template Generator  
4 Implement SubCluster Template System  
5 Integrate generator with Cluster Domain  
6 Implement unit tests  
7 Provide debug endpoints  

---

# LOCATION

Create module:

```
src/platform/cluster-templates/
```

Structure:

```
src/platform/cluster-templates/

models/
templates/
registry/
generator/
services/
```

Create project:

```
Whycespace.ClusterTemplatePlatform.csproj
```

Target framework:

```
net8.0
```

References:

```
Whycespace.ClusterDomain
Whycespace.Contracts
```

---

# CLUSTER TEMPLATE MODEL

Folder:

```
models/
```

Create:

```
ClusterTemplate.cs
```

Fields:

```
TemplateName
ClusterName
SubClusters
DefaultProviders
```

Example:

```csharp
public sealed class ClusterTemplate
{
    public string TemplateName { get; }

    public string ClusterName { get; }

    public IReadOnlyCollection<string> SubClusters { get; }

    public IReadOnlyCollection<string> DefaultProviders { get; }

    public ClusterTemplate(
        string templateName,
        string clusterName,
        IReadOnlyCollection<string> subClusters,
        IReadOnlyCollection<string> providers)
    {
        TemplateName = templateName;
        ClusterName = clusterName;
        SubClusters = subClusters;
        DefaultProviders = providers;
    }
}
```

---

# TEMPLATE REGISTRY

Folder:

```
registry/
```

Create:

```
ClusterTemplateRegistry.cs
```

Purpose:

Store cluster templates.

Example storage:

```
Dictionary<string, ClusterTemplate>
```

Methods:

```
RegisterTemplate
GetTemplate
ListTemplates
```

Example:

```csharp
public sealed class ClusterTemplateRegistry
{
    public void RegisterTemplate(ClusterTemplate template);

    public ClusterTemplate GetTemplate(string templateName);
}
```

---

# CLUSTER TEMPLATE GENERATOR

Folder:

```
generator/
```

Create:

```
ClusterTemplateGenerator.cs
```

Purpose:

Generate clusters from templates.

The generator must use domain services.

Flow:

```
Load template
↓
ClusterAdministrationService.RegisterCluster
↓
ClusterAdministrationService.AddSubCluster
↓
ProviderRegistry.RegisterProvider
↓
ProviderAssignmentService.AssignProvider
```

Example method:

```
GenerateCluster(string templateName)
```

---

# SUBCLUSTER TEMPLATE SYSTEM

Folder:

```
templates/
```

Create:

```
SubClusterTemplate.cs
```

Fields:

```
SubClusterName
DefaultProviders
```

Example:

Taxi

Providers:

```
DriverProvider
VehicleProvider
```

---

# SAMPLE CLUSTER TEMPLATE

Example:

MobilityTemplate

Cluster:

```
WhyceMobility
```

SubClusters:

```
Taxi
RideSharing
```

Providers:

```
DriverProvider
VehicleProvider
```

---

# GENERATION FLOW

Operator requests cluster generation.

Example:

```
GenerateCluster("MobilityTemplate")
```

Flow:

```
TemplateRegistry
↓
ClusterTemplateGenerator
↓
Register cluster
↓
Create subclusters
↓
Register providers
↓
Assign providers
```

---

# PILOT CLUSTER TEMPLATES

Create templates.

MobilityTemplate

Cluster:

```
WhyceMobility
```

SubClusters:

```
Taxi
```

Providers:

```
DriverProvider
VehicleProvider
```

---

PropertyTemplate

Cluster:

```
WhyceProperty
```

SubClusters:

```
LettingAgent
```

Providers:

```
PropertyManagerProvider
MaintenanceProvider
```

---

# UNIT TESTS

Create project:

```
tests/cluster-template/
```

Tests:

```
ClusterTemplateTests.cs
TemplateRegistryTests.cs
ClusterGeneratorTests.cs
```

Test cases:

```
register cluster template
generate cluster from template
generate subclusters correctly
```

---

# DEBUG ENDPOINTS

Add endpoints.

GET

```
/dev/cluster-templates
```

Return available templates.

Example:

```json
{
  "templates": [
    "MobilityTemplate",
    "PropertyTemplate"
  ]
}
```

---

POST

```
/dev/cluster-templates/generate
```

Generate cluster from template.

Example response:

```json
{
  "cluster": "WhyceMobility",
  "subclusters": ["Taxi"]
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
3 passed
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
3 passed
0 failed
```

---

# PHASE COMPLETION CRITERIA

Phase 1.14.5 is complete when:

• cluster templates register correctly  
• cluster generation works  
• subclusters generate automatically  
• providers register automatically  
• tests pass  
• debug endpoints respond  

End of Phase 1.14.5.