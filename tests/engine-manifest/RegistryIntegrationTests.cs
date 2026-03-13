namespace Whycespace.EngineManifest.Tests;

using Whycespace.EngineManifest.Loader;
using Whycespace.EngineManifest.Models;
using Whycespace.EngineRuntime.Registry;
using Whycespace.Engines.T3I.Clusters.Mobility.Taxi;

public class RegistryIntegrationTests
{
    [Fact]
    public void GetDescriptors_ContainsInstanceAndMetadata()
    {
        var registry = new EngineRegistry();
        var loader = new EngineManifestLoader(registry);

        loader.LoadFromAssembly(typeof(DriverMatchingEngine).Assembly);

        var descriptors = loader.GetDescriptors();
        var driverDesc = descriptors.FirstOrDefault(d => d.Metadata.EngineName == "DriverMatching");

        Assert.NotNull(driverDesc);
        Assert.NotNull(driverDesc!.Instance);
        Assert.Equal("DriverMatching", driverDesc.Instance.Name);
        Assert.Equal(EngineTier.T3I, driverDesc.Metadata.Tier);
    }

    [Fact]
    public void AllTiers_Represented()
    {
        var registry = new EngineRegistry();
        var loader = new EngineManifestLoader(registry);

        loader.LoadFromAssembly(typeof(DriverMatchingEngine).Assembly);

        var tiers = loader.GetManifests()
            .Select(m => m.Tier)
            .Distinct()
            .OrderBy(t => t)
            .ToList();

        // T0U and T1M engines are in separate assemblies (Whycespace.Engines.T0U.* and T1M)
        // so they are not discovered when scanning the Whycespace.Engines assembly.
        Assert.Contains(EngineTier.T2E, tiers);
        Assert.Contains(EngineTier.T3I, tiers);
        Assert.Contains(EngineTier.T4A, tiers);
    }

    [Fact]
    public void AllKinds_Represented()
    {
        var registry = new EngineRegistry();
        var loader = new EngineManifestLoader(registry);

        loader.LoadFromAssembly(typeof(DriverMatchingEngine).Assembly);

        var kinds = loader.GetManifests()
            .Select(m => m.Kind)
            .Distinct()
            .OrderBy(k => k)
            .ToList();

        Assert.Contains(EngineKind.Decision, kinds);
        Assert.Contains(EngineKind.Mutation, kinds);
        Assert.Contains(EngineKind.Validation, kinds);
        Assert.Contains(EngineKind.Projection, kinds);
    }
}
