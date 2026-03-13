namespace Whycespace.EngineManifest.Tests;

using Whycespace.EngineManifest.Models;
using Whycespace.EngineManifest.Registry;
using Whycespace.EngineManifest.Loader;
using Whycespace.EngineManifest.Validation;

public class EngineManifestRuntimeTests
{
    private static EngineRuntimeManifest CreateManifest(
        string name = "CapitalContributionEngine",
        string type = "T2E",
        string input = "CapitalContributionRequest",
        string output = "CapitalContributionResult",
        string version = "1.0",
        IReadOnlyList<EngineCapability>? capabilities = null)
    {
        capabilities ??= new List<EngineCapability>
        {
            new("CapitalContribution"),
            new("AssetRegistration")
        };

        return new EngineRuntimeManifest(name, type, input, output, capabilities, version);
    }

    // --- EngineCapability Tests ---

    [Fact]
    public void EngineCapability_StoresName()
    {
        var capability = new EngineCapability("CapitalContribution");
        Assert.Equal("CapitalContribution", capability.Name);
    }

    [Fact]
    public void EngineCapability_ThrowsOnEmptyName()
    {
        Assert.Throws<ArgumentException>(() => new EngineCapability(""));
    }

    [Fact]
    public void EngineCapability_EqualityByName()
    {
        var a = new EngineCapability("CapitalContribution");
        var b = new EngineCapability("CapitalContribution");
        Assert.Equal(a, b);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    // --- EngineRuntimeManifest Tests ---

    [Fact]
    public void Manifest_CreatedWithCorrectProperties()
    {
        var manifest = CreateManifest();

        Assert.Equal("CapitalContributionEngine", manifest.EngineName);
        Assert.Equal("T2E", manifest.EngineType);
        Assert.Equal("CapitalContributionRequest", manifest.InputContract);
        Assert.Equal("CapitalContributionResult", manifest.OutputContract);
        Assert.Equal("1.0", manifest.Version);
        Assert.Equal(2, manifest.Capabilities.Count);
    }

    // --- EngineManifestRegistry Tests ---

    [Fact]
    public void Registry_RegisterAndGet()
    {
        var registry = new EngineManifestRegistry();
        var manifest = CreateManifest();

        registry.Register(manifest);
        var result = registry.Get("CapitalContributionEngine");

        Assert.Equal("CapitalContributionEngine", result.EngineName);
    }

    [Fact]
    public void Registry_DuplicateRegistration_Throws()
    {
        var registry = new EngineManifestRegistry();
        var manifest = CreateManifest();

        registry.Register(manifest);

        var ex = Assert.Throws<InvalidOperationException>(() => registry.Register(manifest));
        Assert.Contains("already registered", ex.Message);
    }

    [Fact]
    public void Registry_GetUnknownEngine_Throws()
    {
        var registry = new EngineManifestRegistry();

        var ex = Assert.Throws<InvalidOperationException>(() => registry.Get("NonExistent"));
        Assert.Contains("not found", ex.Message);
    }

    [Fact]
    public void Registry_GetAll_ReturnsAllManifests()
    {
        var registry = new EngineManifestRegistry();
        registry.Register(CreateManifest("Engine1"));
        registry.Register(CreateManifest("Engine2"));

        var all = registry.GetAll();
        Assert.Equal(2, all.Count);
    }

    [Fact]
    public void Registry_Contains_ReturnsTrueForRegistered()
    {
        var registry = new EngineManifestRegistry();
        registry.Register(CreateManifest());

        Assert.True(registry.Contains("CapitalContributionEngine"));
        Assert.False(registry.Contains("NonExistent"));
    }

    [Fact]
    public void Registry_Count_ReturnsCorrectCount()
    {
        var registry = new EngineManifestRegistry();
        Assert.Equal(0, registry.Count);

        registry.Register(CreateManifest("A"));
        registry.Register(CreateManifest("B"));
        Assert.Equal(2, registry.Count);
    }

    // --- RuntimeManifestLoader Tests ---

    [Fact]
    public void Loader_LoadsManifestsIntoRegistry()
    {
        var registry = new EngineManifestRegistry();
        var loader = new RuntimeManifestLoader(registry);

        var manifests = new[]
        {
            CreateManifest("Engine1"),
            CreateManifest("Engine2"),
            CreateManifest("Engine3")
        };

        loader.Load(manifests);

        Assert.Equal(3, registry.Count);
        Assert.NotNull(registry.Get("Engine1"));
        Assert.NotNull(registry.Get("Engine2"));
        Assert.NotNull(registry.Get("Engine3"));
    }

    // --- RuntimeManifestValidator Tests ---

    [Fact]
    public void Validator_ValidManifest_DoesNotThrow()
    {
        var validator = new RuntimeManifestValidator();
        var manifest = CreateManifest();

        validator.Validate(manifest);
    }

    [Fact]
    public void Validator_EmptyEngineName_Throws()
    {
        var validator = new RuntimeManifestValidator();
        var manifest = CreateManifest(name: "");

        var ex = Assert.Throws<InvalidOperationException>(() => validator.Validate(manifest));
        Assert.Contains("EngineName", ex.Message);
    }

    [Fact]
    public void Validator_EmptyEngineType_Throws()
    {
        var validator = new RuntimeManifestValidator();
        var manifest = CreateManifest(type: "");

        var ex = Assert.Throws<InvalidOperationException>(() => validator.Validate(manifest));
        Assert.Contains("EngineType", ex.Message);
    }

    [Fact]
    public void Validator_EmptyInputContract_Throws()
    {
        var validator = new RuntimeManifestValidator();
        var manifest = CreateManifest(input: "");

        var ex = Assert.Throws<InvalidOperationException>(() => validator.Validate(manifest));
        Assert.Contains("InputContract", ex.Message);
    }

    [Fact]
    public void Validator_EmptyOutputContract_Throws()
    {
        var validator = new RuntimeManifestValidator();
        var manifest = CreateManifest(output: "");

        var ex = Assert.Throws<InvalidOperationException>(() => validator.Validate(manifest));
        Assert.Contains("OutputContract", ex.Message);
    }

    [Fact]
    public void Validator_EmptyVersion_Throws()
    {
        var validator = new RuntimeManifestValidator();
        var manifest = CreateManifest(version: "");

        var ex = Assert.Throws<InvalidOperationException>(() => validator.Validate(manifest));
        Assert.Contains("Version", ex.Message);
    }

    [Fact]
    public void Validator_NoCapabilities_Throws()
    {
        var validator = new RuntimeManifestValidator();
        var manifest = CreateManifest(capabilities: new List<EngineCapability>());

        var ex = Assert.Throws<InvalidOperationException>(() => validator.Validate(manifest));
        Assert.Contains("capability", ex.Message);
    }

    // --- Immutability Tests ---

    [Fact]
    public void Manifest_ImmutableAfterRegistration()
    {
        var registry = new EngineManifestRegistry();
        var manifest = CreateManifest();
        registry.Register(manifest);

        var retrieved = registry.Get("CapitalContributionEngine");
        Assert.Same(manifest, retrieved);
    }
}
