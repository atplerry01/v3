# WHYCESPACE WBSM v3
# PHASE 1.6.5 — ENGINE MANIFEST LOADER

You are implementing **Phase 1.6.5 of the Whycespace system**.

This phase introduces the **Engine Manifest Loader**.

The Engine Manifest Loader automatically discovers engines and loads them into the runtime registry.

This removes the need for manual engine registration.

All engines must provide metadata describing:

• engine name  
• engine type  
• engine tier  
• input contract  
• output events  

---

# OBJECTIVES

1 Implement Engine Manifest  
2 Implement Engine Metadata Model  
3 Implement Engine Manifest Loader  
4 Implement Engine Registry Auto-Registration  
5 Validate engine metadata  
6 Implement unit tests  
7 Provide debug endpoints  

---

# LOCATION

Create module:

```
src/runtime/engine-manifest/
```

Structure:

```
src/runtime/engine-manifest/

├── manifest/
├── loader/
├── registry/
├── models/
└── validation/
```

Create project:

```
Whycespace.EngineManifest.csproj
```

Target framework:

```
net8.0
```

References:

```
Whycespace.Contracts
Whycespace.EngineRuntime
```

---

# ENGINE METADATA MODEL

Folder:

```
models/
```

Create:

```
EngineMetadata.cs
```

Fields:

```
EngineName
EngineTier
EngineType
InputContract
OutputEvents
```

Example:

```csharp
public sealed class EngineMetadata
{
    public string EngineName { get; }

    public string EngineTier { get; }

    public string EngineType { get; }

    public string InputContract { get; }

    public IReadOnlyCollection<string> OutputEvents { get; }

    public EngineMetadata(
        string name,
        string tier,
        string type,
        string input,
        IReadOnlyCollection<string> outputs)
    {
        EngineName = name;
        EngineTier = tier;
        EngineType = type;
        InputContract = input;
        OutputEvents = outputs;
    }
}
```

---

# ENGINE MANIFEST ATTRIBUTE

Folder:

```
manifest/
```

Create:

```
EngineManifestAttribute.cs
```

Purpose:

Attach metadata to engines.

Example:

```csharp
[AttributeUsage(AttributeTargets.Class)]
public sealed class EngineManifestAttribute : Attribute
{
    public string EngineName { get; }

    public string Tier { get; }

    public string Type { get; }

    public EngineManifestAttribute(
        string engineName,
        string tier,
        string type)
    {
        EngineName = engineName;
        Tier = tier;
        Type = type;
    }
}
```

---

# ENGINE MANIFEST LOADER

Folder:

```
loader/
```

Create:

```
EngineManifestLoader.cs
```

Purpose:

Scan assemblies and discover engines.

Responsibilities:

• scan assemblies  
• detect EngineManifestAttribute  
• create EngineMetadata  
• register engine  

Example:

```csharp
public sealed class EngineManifestLoader
{
    public IReadOnlyCollection<EngineMetadata> LoadFromAssembly(
        Assembly assembly)
    {
        // scan assembly
    }
}
```

---

# ENGINE REGISTRY INTEGRATION

Modify:

```
EngineRegistry
```

Instead of manual registration:

```
EngineManifestLoader
```

will load engines automatically.

Example flow:

```
Application Startup
 ↓
EngineManifestLoader
 ↓
EngineMetadata
 ↓
EngineRegistry.Register()
```

---

# ENGINE VALIDATION

Folder:

```
validation/
```

Create:

```
EngineManifestValidator.cs
```

Validation rules:

• engine must implement `IEngine`  
• engine must have `EngineManifestAttribute`  
• engine must declare output events  
• engine must declare input contract  

---

# SAMPLE ENGINE METADATA

Example engine:

```
DriverMatchingEngine
```

Manifest:

```csharp
[EngineManifest(
    "DriverMatchingEngine",
    "T2E",
    "Decision"
)]
public sealed class DriverMatchingEngine : IEngine
{
}
```

---

# RUNTIME ENGINE DISCOVERY FLOW

```
Application Startup
 ↓
Assembly Scan
 ↓
EngineManifestLoader
 ↓
EngineMetadata
 ↓
EngineRegistry
 ↓
Runtime Dispatcher
```

---

# UNIT TESTS

Create project:

```
tests/engine-manifest/
```

Tests:

```
EngineMetadataTests.cs
ManifestLoaderTests.cs
EngineRegistryIntegrationTests.cs
```

Test cases:

```
load engine metadata
detect engine manifest attribute
register engines automatically
```

---

# DEBUG ENDPOINTS

Add endpoints.

GET

```
/dev/engines/manifests
```

Return discovered engine metadata.

Example:

```json
{
  "engines": [
    {
      "name": "DriverMatchingEngine",
      "tier": "T2E"
    },
    {
      "name": "RevenueRecordingEngine",
      "tier": "T2E"
    }
  ]
}
```

---

GET

```
/dev/engines/registry
```

Return engines loaded in runtime.

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

Phase 1.6.5 is complete when:

• engine metadata loads automatically  
• engine manifests validated  
• engines registered dynamically  
• tests pass  
• debug endpoints respond  

End of Phase 1.6.5.