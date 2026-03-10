namespace Whycespace.EngineManifest.Tests;

using System.Reflection;
using Whycespace.Contracts.Engines;
using Whycespace.EngineManifest.Manifest;
using Whycespace.EngineManifest.Models;
using Whycespace.Engines.T3I_Intelligence;

public class EngineMetadataTests
{
    [Fact]
    public void EngineManifestAttribute_ParsedCorrectly()
    {
        var attr = typeof(DriverMatchingEngine).GetCustomAttribute<EngineManifestAttribute>();

        Assert.NotNull(attr);
        Assert.Equal("DriverMatching", attr!.EngineName);
        Assert.Equal(EngineTier.T3I, attr.Tier);
        Assert.Equal(EngineKind.Decision, attr.Kind);
        Assert.Equal("DriverMatchingRequest", attr.InputContract);
        Assert.Single(attr.OutputEvents);
        Assert.Equal(typeof(EngineEvent), attr.OutputEvents[0]);
    }

    [Fact]
    public void EngineMetadata_CreatedFromAttribute()
    {
        var attr = typeof(DriverMatchingEngine).GetCustomAttribute<EngineManifestAttribute>()!;

        var metadata = new EngineMetadata(
            EngineName: attr.EngineName,
            Tier: attr.Tier,
            Kind: attr.Kind,
            InputContract: attr.InputContract,
            OutputEvents: attr.OutputEvents.Select(t => t.Name).ToList().AsReadOnly()
        );

        Assert.Equal("DriverMatching", metadata.EngineName);
        Assert.Equal(EngineTier.T3I, metadata.Tier);
        Assert.Equal(EngineKind.Decision, metadata.Kind);
        Assert.Equal("DriverMatchingRequest", metadata.InputContract);
        Assert.Contains("EngineEvent", metadata.OutputEvents);
    }

    [Fact]
    public void AllEngines_HaveManifestAttribute()
    {
        var assembly = typeof(DriverMatchingEngine).Assembly;
        var engineTypes = assembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false }
                        && typeof(IEngine).IsAssignableFrom(t));

        foreach (var type in engineTypes)
        {
            var attr = type.GetCustomAttribute<EngineManifestAttribute>();
            Assert.True(attr is not null, $"Engine '{type.Name}' is missing [EngineManifest] attribute.");
        }
    }
}
