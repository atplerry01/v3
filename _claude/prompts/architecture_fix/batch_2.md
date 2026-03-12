# WHYCESPACE WBSM v3
# ARCHITECTURE FIX PROMPT — BATCH 2
# ENGINE ORCHESTRATION CLEANUP

You are refactoring the Whycespace codebase to remove **direct engine-to-engine orchestration chains**.

This is a critical architecture fix required by WBSM v3.

Engines must never orchestrate other engines directly.

---

# OBJECTIVE

Remove all **direct engine construction and invocation chains** such as:

EngineA → new EngineB(...)  
EngineA → EngineB.Execute(...)

Engines must instead:

• depend on **interfaces**
• receive dependencies via **dependency injection**
• or be orchestrated through the **runtime layer**

Correct architecture:

Runtime Dispatcher
        ↓
Engine A
        ↓
Event / Decision
        ↓
Engine B

Incorrect architecture:

Engine A → Engine B

---

# TARGET FILES

The following engines currently construct other engines directly:

src/engines/T0U/WhycePolicy/PolicyImpactForecastEngine.cs  
src/engines/T0U/WhycePolicy/PolicySimulationEngine.cs  
src/engines/T0U/WhycePolicy/PolicyEvaluationEngine.cs  
src/engines/T1M/WSS/definition/WorkflowVersioningEngine.cs

These must be refactored.

---

# STEP 1 — REMOVE DIRECT ENGINE CONSTRUCTION

Remove patterns like:

new PolicySimulationEngine(...)
new PolicyEvaluationEngine(...)
new PolicyDependencyEngine(...)

Engines must not construct other engines.

---

# STEP 2 — USE INTERFACES

Define engine interfaces if not already present.

Example:

IPolicySimulationEngine  
IPolicyEvaluationEngine  
IPolicyDependencyEngine  

Replace concrete dependencies with interfaces.

Example:

Before:

private readonly PolicyEvaluationEngine _evaluationEngine;

After:

private readonly IPolicyEvaluationEngine _evaluationEngine;

---

# STEP 3 — USE DEPENDENCY INJECTION

Dependencies must be injected via constructor.

Example:

Before:

var simulation = new PolicySimulationEngine(store);

After:

constructor:

PolicyImpactForecastEngine(
    IPolicySimulationEngine simulationEngine
)

Usage:

var result = simulationEngine.Simulate(policy);

---

# STEP 4 — REMOVE ORCHESTRATION LOGIC FROM ENGINES

Engines must not coordinate execution pipelines.

Example incorrect flow:

ForecastEngine
    ↓
SimulationEngine
    ↓
EvaluationEngine
    ↓
DependencyEngine

Instead:

Each engine performs **a single deterministic operation**.

Runtime or orchestration layers decide which engine runs next.

---

# STEP 5 — SPLIT PIPELINE RESPONSIBILITY

Refactor engines to operate as pure processors.

Example:

PolicySimulationEngine

Input:
Policy + Scenario

Output:
SimulationResult

PolicyEvaluationEngine

Input:
PolicyContext

Output:
PolicyDecision

PolicyDependencyEngine

Input:
PolicyGraph

Output:
DependencyResolution

No engine should invoke the next stage.

---

# STEP 6 — REGISTER ENGINES IN DI

Update dependency injection configuration.

Example:

builder.Services.AddSingleton<IPolicySimulationEngine, PolicySimulationEngine>();
builder.Services.AddSingleton<IPolicyEvaluationEngine, PolicyEvaluationEngine>();
builder.Services.AddSingleton<IPolicyDependencyEngine, PolicyDependencyEngine>();

This allows orchestration layers to compose engines.

---

# STEP 7 — FIX WORKFLOW VERSIONING ENGINE

File:

src/engines/T1M/WSS/definition/WorkflowVersioningEngine.cs

Remove direct instantiation of versioning engines.

Inject interface instead.

Example:

Before:

var versionEngine = new WorkflowVersioningEngine(store);

After:

constructor injection:

WorkflowDefinitionEngine(
    IWorkflowVersioningEngine versioningEngine
)

---

# STEP 8 — VERIFY ENGINE RULES

After refactoring verify:

• no engine creates another engine  
• engines depend only on interfaces  
• engines contain no orchestration pipelines  
• orchestration responsibility moves to runtime layer  

---

# VALIDATION CHECKLIST

Confirm the following before completing:

No engine file contains:

new <AnotherEngine>(...)

All engine dependencies are interfaces.

All engines remain:

stateless  
thread-safe  
deterministic  

Runtime or higher orchestration layers coordinate engine sequencing.

---

# EXPECTED RESULT

Build succeeds.

0 warnings  
0 errors  

All tests pass.

Engine architecture becomes compliant with WBSM v3.

---

# IMPORTANT

Do NOT move engine files.

Do NOT modify domain logic.

Do NOT modify runtime dispatcher yet.

This task only removes **engine orchestration chains**.

---

# END OF TASK