# WHYCESPACE WBSM v3
# PHASE 1.2 — COMMAND SYSTEM

You are implementing **Phase 1.2 of the Whycespace system**.

This phase implements the **Command System**, which acts as the entry point for all state-changing operations in the system.

All mutations must follow the canonical flow:

```
API
 ↓
Command
 ↓
Workflow
 ↓
Engine
 ↓
Event
```

Commands must be:

• immutable  
• idempotent  
• validated  
• traceable  

The command system is **runtime infrastructure**, not a system.

---

# OBJECTIVES

1 Implement Command Catalog  
2 Implement Command Dispatcher  
3 Implement Command Validator  
4 Implement Idempotency Registry  
5 Implement Command Routing  
6 Create unit tests  
7 Add debug endpoints  

---

# LOCATION

Create the command runtime module inside:

```
src/runtime/command/
```

Structure:

```
src/runtime/command/

├── catalog/
├── dispatcher/
├── validation/
├── idempotency/
├── routing/
└── models/
```

Create project:

```
Whycespace.CommandSystem.csproj
```

Target framework:

```
net8.0
```

Reference project:

```
Whycespace.Contracts
```

---

# COMMAND MODEL

Folder:

```
models/
```

Create:

```
CommandEnvelope.cs
```

Purpose:

Wrap incoming command with metadata.

Example:

```csharp
public sealed record CommandEnvelope(
    Guid CommandId,
    string CommandType,
    object Payload,
    Timestamp Timestamp
);
```

---

# COMMAND CATALOG

Folder:

```
catalog/
```

Create:

```
ICommandCatalog.cs
```

```csharp
public interface ICommandCatalog
{
    void Register(string commandType, Type commandHandler);
    Type? Resolve(string commandType);
}
```

Create implementation:

```
CommandCatalog.cs
```

Use in-memory dictionary:

```
Dictionary<string, Type>
```

Methods:

```
Register
Resolve
```

---

# COMMAND VALIDATOR

Folder:

```
validation/
```

Create:

```
ICommandValidator.cs
```

```csharp
public interface ICommandValidator
{
    void Validate(CommandEnvelope command);
}
```

Create:

```
CommandValidator.cs
```

Validation rules:

• CommandId must not be empty  
• CommandType must not be empty  
• Payload must not be null  

Throw:

```
InvalidOperationException
```

on validation failure.

---

# IDEMPOTENCY REGISTRY

Folder:

```
idempotency/
```

Create:

```
IIdempotencyRegistry.cs
```

```csharp
public interface IIdempotencyRegistry
{
    bool Exists(Guid commandId);
    void Register(Guid commandId);
}
```

Create:

```
InMemoryIdempotencyRegistry.cs
```

Use:

```
HashSet<Guid>
```

Behavior:

• Reject duplicate command IDs  
• Register new commands  

---

# COMMAND ROUTER

Folder:

```
routing/
```

Create:

```
ICommandRouter.cs
```

```csharp
public interface ICommandRouter
{
    string ResolveWorkflow(string commandType);
}
```

Create implementation:

```
CommandRouter.cs
```

Use simple dictionary mapping.

Example mappings:

```
RequestRideCommand → TaxiRideRequestWorkflow
CreatePropertyListingCommand → PropertyListingWorkflow
```

---

# COMMAND DISPATCHER

Folder:

```
dispatcher/
```

Create:

```
CommandDispatcher.cs
```

Dependencies:

```
CommandValidator
IdempotencyRegistry
CommandRouter
```

Flow:

```
1 Validate command
2 Check idempotency
3 Register command
4 Resolve workflow
5 Return WorkflowExecutionRequest
```

Example signature:

```csharp
public WorkflowExecutionRequest Dispatch(CommandEnvelope command)
```

---

# COMMAND EXAMPLE

Create example command:

```
models/RequestRideCommand.cs
```

Example:

```csharp
public sealed record RequestRideCommand(
    Guid CommandId,
    Guid RiderId,
    string PickupLocation
) : CommandBase(CommandId, Timestamp.Now());
```

---

# ADD PROJECT TO SOLUTION

Add project:

```
Whycespace.CommandSystem
```

Reference:

```
Whycespace.Contracts
```

---

# UNIT TESTS

Create test folder:

```
tests/command/
```

Add project:

```
Whycespace.CommandSystem.Tests
```

Create tests:

```
CommandValidatorTests.cs
IdempotencyRegistryTests.cs
CommandRouterTests.cs
CommandDispatcherTests.cs
```

Test cases:

• Command validation success  
• Duplicate command rejection  
• Command routing  
• Dispatcher workflow resolution  

---

# DEBUG ENDPOINTS

Add endpoints in FoundationHost.

GET

```
/dev/commands
```

Return:

```json
{
  "commands": [
    "RequestRideCommand",
    "CreatePropertyListingCommand"
  ]
}
```

POST

```
/dev/commands/dispatch
```

Accept:

```
CommandEnvelope
```

Return:

```
WorkflowExecutionRequest
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

1 Files Created  
2 Repository Tree  
3 Build Result  
4 Test Result  
5 Debug Endpoints  

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

Phase 1.2 is complete when:

• command system compiles  
• dispatcher works  
• idempotency protection works  
• commands route to workflows  
• tests pass  
• debug endpoint works  

End of Phase 1.2.