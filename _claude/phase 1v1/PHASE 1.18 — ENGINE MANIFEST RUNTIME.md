# WHYCESPACE WBSM v3
# PHASE 1.18 — ENGINE MANIFEST RUNTIME

You are implementing **Phase 1.18 of the Whycespace system**.

This phase introduces the **Engine Manifest Runtime**, which allows the runtime to:

• discover engines dynamically  
• inspect engine capabilities  
• validate workflow engine steps  
• provide runtime engine metadata  

This phase DOES NOT modify the engine execution system.

It only introduces **engine metadata and registry infrastructure**.

---

# ARCHITECTURE RULES

Follow WBSM v3 rules strictly.

1️⃣ Engines NEVER call engines.

2️⃣ Runtime invokes engines.

3️⃣ Engine metadata must be deterministic.

4️⃣ Engine manifests must be immutable once registered.

5️⃣ No external libraries.

6️⃣ All classes must be sealed.

---

# LOCATION

Create module:

src/runtime/engine-manifest/

Structure:

src/runtime/engine-manifest/

├── models/
├── registry/
├── loader/
├── validation/
└── interfaces/

Create project:

Whycespace.EngineManifestRuntime.csproj

Target framework:

net8.0

Project references:

Whycespace.Contracts

---

# OBJECTIVES

Implement:

1️⃣ EngineCapability  
2️⃣ EngineManifest  
3️⃣ EngineManifestRegistry  
4️⃣ EngineManifestLoader  
5️⃣ EngineManifestValidator  

Add unit tests.

Add debug endpoint.

---

# ENGINE CAPABILITY

Create:

models/EngineCapability.cs

Purpose:

Represents capabilities supported by an engine.

Example:

CapitalContribution  
AssetRegistration  
RevenueRecording  
ProfitDistribution

Implementation:

public sealed class EngineCapability
{
    public string Name { get; }

    public EngineCapability(string name)
    {
        Name = name;
    }
}

---

# ENGINE MANIFEST

Create:

models/EngineManifest.cs

Purpose:

Defines metadata about an engine.

Implementation:

public sealed class EngineManifest
{
    public string EngineName { get; }

    public string EngineType { get; }

    public string InputContract { get; }

    public string OutputContract { get; }

    public IReadOnlyList<EngineCapability> Capabilities { get; }

    public string Version { get; }

    public EngineManifest(
        string engineName,
        string engineType,
        string inputContract,
        string outputContract,
        IReadOnlyList<EngineCapability> capabilities,
        string version)
    {
        EngineName = engineName;
        EngineType = engineType;
        InputContract = inputContract;
        OutputContract = outputContract;
        Capabilities = capabilities;
        Version = version;
    }
}

---

# ENGINE MANIFEST REGISTRY

Create:

registry/EngineManifestRegistry.cs

Purpose:

Stores all engine manifests.

Implementation:

public sealed class EngineManifestRegistry
{
    private readonly Dictionary<string, EngineManifest> _manifests
        = new();

    public void Register(EngineManifest manifest)
    {
        if (_manifests.ContainsKey(manifest.EngineName))
            throw new InvalidOperationException("Engine already registered");

        _manifests[manifest.EngineName] = manifest;
    }

    public EngineManifest Get(string engineName)
    {
        if (!_manifests.TryGetValue(engineName, out var manifest))
            throw new InvalidOperationException("Engine not found");

        return manifest;
    }

    public IReadOnlyCollection<EngineManifest> GetAll()
    {
        return _manifests.Values.ToList().AsReadOnly();
    }
}

---

# ENGINE MANIFEST LOADER

Create:

loader/EngineManifestLoader.cs

Purpose:

Loads engine manifests at runtime.

Implementation:

public sealed class EngineManifestLoader
{
    private readonly EngineManifestRegistry _registry;

    public EngineManifestLoader(EngineManifestRegistry registry)
    {
        _registry = registry;
    }

    public void Load(IEnumerable<EngineManifest> manifests)
    {
        foreach (var manifest in manifests)
        {
            _registry.Register(manifest);
        }
    }
}

---

# ENGINE MANIFEST VALIDATOR

Create:

validation/EngineManifestValidator.cs

Purpose:

Validates engine manifests before registration.

Implementation:

public sealed class EngineManifestValidator
{
    public void Validate(EngineManifest manifest)
    {
        if (string.IsNullOrWhiteSpace(manifest.EngineName))
            throw new InvalidOperationException("EngineName required");

        if (string.IsNullOrWhiteSpace(manifest.EngineType))
            throw new InvalidOperationException("EngineType required");

        if (string.IsNullOrWhiteSpace(manifest.InputContract))
            throw new InvalidOperationException("InputContract required");

        if (string.IsNullOrWhiteSpace(manifest.OutputContract))
            throw new InvalidOperationException("OutputContract required");
    }
}

---

# DEBUG ENDPOINT

Add endpoint:

/dev/runtime/engine-manifests

Returns:

• registered engines  
• capabilities  
• contracts  

Example response:

[
  {
    "engineName": "CapitalContributionEngine",
    "engineType": "T2E",
    "inputContract": "CapitalContributionRequest",
    "outputContract": "CapitalContributionResult",
    "version": "1.0"
  }
]

---

# UNIT TESTS

Create project:

tests/runtime/engine-manifest/

Add tests:

EngineManifestTests.cs

Tests:

• manifest creation  
• duplicate registration rejection  
• manifest lookup  
• manifest validation  

---

# BUILD SUCCESS CRITERIA

Build succeeds with:

0 errors  
0 warnings  

All tests pass.

Debug endpoint returns registered manifests.

---

# PHASE RESULT

After this phase the runtime can:

• discover engines  
• inspect capabilities  
• validate workflow engine steps  
• expose engine metadata for tooling

This prepares the system for:

Phase 1.19 — Engine Worker Pools.