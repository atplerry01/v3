# WHYCESPACE WBSM v3

# PHASE 2.1.1 — WORKFLOW DEFINITION ENGINE
(Midstream System Architecture)

You are implementing the **Workflow Definition Engine** of the WSS (Workflow Structural System).

This module defines workflow structures used by the orchestration system.

Workflows describe **process flows**, not business logic.

Example:

Taxi Ride Workflow

RequestRide → FindDriver → DriverAccept → TripStart → TripComplete → Payment

Important architecture rule:

ENGINES are stateless  
WORKFLOW STATE is stored by the system

---

# OBJECTIVES

Implement:

SYSTEM COMPONENTS

• WorkflowDefinition
• WorkflowStepDefinition
• WorkflowMetadata

ENGINE COMPONENT

• WorkflowDefinitionEngine

Also implement:

• Commands
• Unit tests

---

# MODULE LOCATION

SYSTEM MODULE

src/system/midstream/WSS/

Create folder:

definitions/

Structure:

definitions/

├── WorkflowDefinition.cs
├── WorkflowStepDefinition.cs
└── WorkflowMetadata.cs

---

ENGINE MODULE

src/engines/T1M_Orchestration/WSS/

Create folder:

definition/

Structure:

definition/

└── WorkflowDefinitionEngine.cs

---

# WORKFLOW STEP DEFINITION

Create:

definitions/WorkflowStepDefinition.cs

```csharp
public sealed class WorkflowStepDefinition
{
    public string StepId { get; }

    public string Name { get; }

    public WorkflowStepDefinition(
        string stepId,
        string name)
    {
        StepId = stepId;
        Name = name;
    }
}
```

---

# WORKFLOW METADATA

Create:

definitions/WorkflowMetadata.cs

```csharp
public sealed class WorkflowMetadata
{
    public string WorkflowId { get; }

    public string Cluster { get; }

    public string SubCluster { get; }

    public string Description { get; }

    public WorkflowMetadata(
        string workflowId,
        string cluster,
        string subCluster,
        string description)
    {
        WorkflowId = workflowId;
        Cluster = cluster;
        SubCluster = subCluster;
        Description = description;
    }
}
```

---

# WORKFLOW DEFINITION

Create:

definitions/WorkflowDefinition.cs

```csharp
public sealed class WorkflowDefinition
{
    public WorkflowMetadata Metadata { get; }

    public IReadOnlyList<WorkflowStepDefinition> Steps { get; }

    public WorkflowDefinition(
        WorkflowMetadata metadata,
        IReadOnlyList<WorkflowStepDefinition> steps)
    {
        Metadata = metadata;
        Steps = steps;
    }
}
```

---

# WORKFLOW DEFINITION ENGINE

Create:

definition/WorkflowDefinitionEngine.cs

```csharp
public sealed class WorkflowDefinitionEngine
{
    public WorkflowDefinition Create(
        WorkflowMetadata metadata,
        IReadOnlyList<WorkflowStepDefinition> steps)
    {
        if (steps == null || steps.Count == 0)
            throw new InvalidOperationException(
                "Workflow must contain at least one step");

        return new WorkflowDefinition(
            metadata,
            steps);
    }
}
```

---

# COMMAND

Create:

commands/CreateWorkflowDefinitionCommand.cs

```csharp
public sealed record CreateWorkflowDefinitionCommand(
    string WorkflowId,
    string Cluster,
    string SubCluster,
    string Description);
```

---

# UNIT TESTS

Create:

tests/WSS.WorkflowDefinition.Tests/

Example:

WorkflowDefinitionTests.cs

```csharp
[Fact]
public void Workflow_ShouldCreateSuccessfully()
{
    var metadata = new WorkflowMetadata(
        "RideWorkflow",
        "Mobility",
        "Taxi",
        "Taxi ride workflow");

    var steps = new List<WorkflowStepDefinition>
    {
        new WorkflowStepDefinition("step1","RequestRide"),
        new WorkflowStepDefinition("step2","FindDriver")
    };

    var engine = new WorkflowDefinitionEngine();

    var workflow = engine.Create(
        metadata,
        steps);

    Assert.Equal("RideWorkflow", workflow.Metadata.WorkflowId);
}
```

---

# BUILD VALIDATION

Run:

dotnet build

Expected:

Build succeeded  
0 errors  
0 warnings  

---

# SUCCESS CRITERIA

Workflow definitions created  
Steps validated  
Metadata stored  
Unit tests pass  

---

# END OF PHASE 2.1.1