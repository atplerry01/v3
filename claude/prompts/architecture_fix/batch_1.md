# WHYCESPACE WBSM v3
# ARCHITECTURE FIX PROMPT — BATCH 1
# PLATFORM → RUNTIME DECOUPLING

You are refactoring the Whycespace codebase to remove a major architecture violation.

This task fixes **platform importing engines directly**.

You must refactor the platform layer so that it interacts only with the **runtime dispatcher**.

Platform must never import engine namespaces.

---

# OBJECTIVE

Fix the following architecture violation:

Platform layer currently imports and constructs engine instances.

This violates WBSM layered architecture.

Correct architecture:

Platform
    ↓
Runtime Dispatcher
    ↓
Engines

Incorrect architecture:

Platform → Engine

---

# FILES TO FIX

The following files currently import engines:

src/platform/controlplane/PolicyMiddleware.cs  
src/platform/controlplane/DebugController.cs  
src/platform/Program.cs

These must be refactored.

---

# STEP 1 — REMOVE ENGINE IMPORTS

Remove all imports of:

Whycespace.Engines.*

From the platform layer.

Example:

REMOVE:

using Whycespace.Engines.T0U.WhycePolicy;
using Whycespace.Engines.T1M.WSS;

Platform must not import engines.

---

# STEP 2 — USE RUNTIME DISPATCHER

Platform must call engines via runtime dispatcher.

The dispatcher already exists inside:

src/runtime/dispatcher/

Use the dispatcher interface:

IRuntimeDispatcher

Inject it via dependency injection.

Example constructor change:

Before:

DebugController(
    PolicyEvaluationEngine policyEngine,
    WorkflowLifecycleEngine lifecycleEngine,
    ...
)

After:

DebugController(
    IRuntimeDispatcher dispatcher
)

---

# STEP 3 — DISPATCH COMMANDS THROUGH RUNTIME

Platform endpoints must dispatch runtime commands.

Example:

Before:

var result = policyEvaluationEngine.Evaluate(policy);

After:

var result = await runtimeDispatcher.DispatchAsync(
    "policy.evaluate",
    payload
);

Do not call engines directly.

---

# STEP 4 — DEBUG CONTROLLER REFACTOR

DebugController currently receives dozens of engine instances.

Refactor so that:

DebugController receives only:

IRuntimeDispatcher

All debug endpoints must use runtime dispatcher to execute engines.

Example:

POST /dev/engines/invoke

Implementation:

await dispatcher.InvokeEngine(
    engineName,
    command,
    payload
)

---

# STEP 5 — POLICY MIDDLEWARE REFACTOR

PolicyMiddleware currently constructs policy engines directly.

Remove engine instantiation.

Replace with dispatcher invocation.

Example:

Before:

var decision = policyEngine.Evaluate(context);

After:

var decision = await dispatcher.DispatchAsync(
    "policy.evaluate",
    context
);

---

# STEP 6 — PROGRAM HOST CLEANUP

File:

src/platform/Program.cs

Program.cs must:

- configure DI
- register runtime dispatcher
- register controllers

Program.cs must NOT instantiate engines directly.

Replace direct engine construction with DI registration.

Example:

builder.Services.AddSingleton<IRuntimeDispatcher, RuntimeDispatcher>();

Remove all direct:

new PolicyEvaluationEngine(...)
new WorkflowLifecycleEngine(...)

---

# STEP 7 — VERIFY PLATFORM LAYER

After refactoring, platform must only depend on:

runtime
shared
system

Platform must NOT depend on:

engines

---

# VALIDATION CHECKLIST

Before completing this task verify:

No platform file imports:

Whycespace.Engines.*

Platform controllers use:

IRuntimeDispatcher

Runtime dispatcher invokes engines.

No engine instances are constructed inside platform.

---

# EXPECTED RESULT

Build succeeds.

0 warnings  
0 errors  

All tests pass.

Architecture becomes:

Platform
    ↓
Runtime Dispatcher
    ↓
Engines

---

# IMPORTANT

Do not change engine logic.

Do not change domain logic.

Only refactor platform invocation flow.

---

# END OF TASK