namespace Whycespace.EngineManifest.Tests;

using Whycespace.Runtime.EngineManifest.Loader;
using Whycespace.Runtime.EngineManifest.Models;
using Whycespace.EngineRuntime.Registry;
using Whycespace.Engines.T2E.Clusters.Mobility.Taxi.Engines;

public class ManifestLoaderTests
{
    [Fact]
    public void LoadFromAssembly_DiscoversAllEngines()
    {
        var registry = new EngineRegistry();
        var loader = new EngineManifestLoader(registry);

        loader.LoadFromAssembly(typeof(DriverMatchingEngine).Assembly);

        var engines = registry.ListEngines();
        Assert.True(engines.Count >= 26, $"Expected at least 26 engines, found {engines.Count}");
    }

    [Fact]
    public void LoadFromAssembly_CreatesMetadata()
    {
        var registry = new EngineRegistry();
        var loader = new EngineManifestLoader(registry);

        loader.LoadFromAssembly(typeof(DriverMatchingEngine).Assembly);

        var manifests = loader.GetManifests();
        Assert.True(manifests.Count >= 26);

        var driverManifest = manifests.FirstOrDefault(m => m.EngineName == "DriverMatching");
        Assert.NotNull(driverManifest);
        Assert.Equal(EngineTier.T3I, driverManifest!.Tier);
        Assert.Equal(EngineKind.Decision, driverManifest.Kind);
    }

    [Fact]
    public void LoadFromAssembly_RegistersEnginesInRegistry()
    {
        var registry = new EngineRegistry();
        var loader = new EngineManifestLoader(registry);

        loader.LoadFromAssembly(typeof(DriverMatchingEngine).Assembly);

        var engine = registry.Resolve("DriverMatching");
        Assert.NotNull(engine);
        Assert.Equal("DriverMatching", engine.Name);
    }
}
