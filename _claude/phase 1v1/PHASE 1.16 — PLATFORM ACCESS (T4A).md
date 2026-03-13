# WHYCESPACE WBSM v3
# PHASE 1.16 — PLATFORM ACCESS (T4A)

You are implementing **Phase 1.16 of the Whycespace system**.

This phase introduces the **T4A Application / Access Layer**.

T4A provides safe external access to the system.

It powers:

• API access  
• Operator control plane  
• Developer tools  
• Integration interfaces  
• Debug surfaces  

T4A must **never mutate system state directly**.

All state mutations must go through:

Commands → Workflows → Engines

---

# OBJECTIVES

1 Implement API Engine  
2 Implement Operator Control Plane Engine  
3 Implement Developer Tools Engine  
4 Implement Integration Engine  
5 Connect T4A with Command System  
6 Implement unit tests  
7 Provide debug endpoints  

---

# ENGINE LOCATION

Create engines under:

```
src/engines/T4A_Access/
```

Structure:

```
src/engines/T4A_Access/

APIEngine/
OperatorControlPlane/
DeveloperToolsEngine/
IntegrationEngine/
```

Create project:

```
Whycespace.AccessEngines.csproj
```

Target framework:

```
net8.0
```

References:

```
Whycespace.Contracts
Whycespace.CommandSystem
Whycespace.Runtime
```

---

# PLATFORM SURFACES

Platform applications expose the system externally.

Create structure:

```
src/platform/

gateway/
   WhyceApiGateway/

controlplane/
   OperatorConsole/

ui/
   WhycePortal/

integrations/
```

Platform surfaces must call **T4A engines**.

---

# API ENGINE

Folder:

```
APIEngine/
```

Create:

```
ApiEngine.cs
```

Purpose:

Translate API requests into commands.

Responsibilities:

• receive API requests  
• construct CommandEnvelope  
• send to CommandDispatcher  

Example method:

```
DispatchCommand(ApiRequest request)
```

---

# OPERATOR CONTROL PLANE ENGINE

Folder:

```
OperatorControlPlane/
```

Create:

```
OperatorControlPlaneEngine.cs
```

Purpose:

Allow operators to manage the system.

Capabilities:

```
CreateCluster
RegisterProvider
CreateSPV
RunSimulation
InspectRuntime
```

---

# DEVELOPER TOOLS ENGINE

Folder:

```
DeveloperToolsEngine/
```

Create:

```
DeveloperToolsEngine.cs
```

Purpose:

Provide developer inspection capabilities.

Capabilities:

```
InspectWorkflowGraph
ListEngines
ListEventStreams
InspectProjections
InspectPartitions
```

---

# INTEGRATION ENGINE

Folder:

```
IntegrationEngine/
```

Create:

```
IntegrationEngine.cs
```

Purpose:

Expose integration interfaces.

Examples:

```
Payment Providers
Identity Providers
External Data APIs
```

Example methods:

```
RegisterExternalProvider
SendExternalEvent
ReceiveExternalEvent
```

---

# PLATFORM API GATEWAY

Create application:

```
src/platform/gateway/WhyceApiGateway/
```

Responsibilities:

```
receive HTTP requests
validate request
forward to ApiEngine
return response
```

Example endpoint:

```
POST /api/commands
```

Payload:

```
CommandEnvelope
```

---

# COMMAND FLOW

Example request:

Taxi ride request.

Flow:

```
HTTP Request
↓
WhyceApiGateway
↓
ApiEngine
↓
CommandDispatcher
↓
Workflow Runtime
↓
Execution Engines
```

---

# OPERATOR CONSOLE

Create application:

```
src/platform/controlplane/OperatorConsole/
```

Capabilities:

```
manage clusters
manage providers
manage SPVs
run simulations
view diagnostics
```

---

# DEVELOPER INSPECTION

Developer tools allow inspection of:

```
workflow graphs
engine registry
event streams
projection state
partition routing
```

---

# UNIT TESTS

Create project:

```
tests/access-engines/
```

Tests:

```
ApiEngineTests.cs
OperatorControlPlaneTests.cs
DeveloperToolsEngineTests.cs
IntegrationEngineTests.cs
```

Test cases:

```
API command routing
operator actions
developer inspection tools
external integration handling
```

---

# DEBUG ENDPOINTS

Add endpoints.

GET

```
/dev/platform/routes
```

Return API routes.

Example:

```
{
 "routes": [
  "/api/commands",
  "/api/workflows"
 ]
}
```

---

GET

```
/dev/platform/tools
```

Return developer tools.

Example:

```
{
 "tools": [
  "workflow-inspector",
  "engine-inspector"
 ]
}
```

---

GET

```
/dev/platform/health
```

Return platform health.

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

Phase 1.16 is complete when:

• API engine routes commands  
• operator console manages system  
• developer tools inspect runtime  
• integrations available  
• tests pass  
• debug endpoints respond  

End of Phase 1.16.