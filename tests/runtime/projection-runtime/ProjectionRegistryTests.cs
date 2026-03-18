using Whycespace.ProjectionRuntime.Registry;

namespace Whycespace.ProjectionRuntime.Tests;

public sealed class ProjectionRegistryTests
{
    [Fact]
    public void Register_AndResolve_ReturnsProjectionName()
    {
        var registry = new ProjectionRegistry();
        registry.Register("CapitalContributionRecorded", "SPVCapitalProjection");

        var result = registry.Resolve("CapitalContributionRecorded");

        Assert.Equal("SPVCapitalProjection", result);
    }

    [Fact]
    public void Resolve_ThrowsWhenNotRegistered()
    {
        var registry = new ProjectionRegistry();

        Assert.Throws<InvalidOperationException>(() => registry.Resolve("UnknownEvent"));
    }

    [Fact]
    public void GetMappings_ReturnsAllRegistered()
    {
        var registry = new ProjectionRegistry();
        registry.Register("Event1", "Projection1");
        registry.Register("Event2", "Projection2");

        var mappings = registry.GetMappings();

        Assert.Equal(2, mappings.Count);
        Assert.Equal("Projection1", mappings["Event1"]);
        Assert.Equal("Projection2", mappings["Event2"]);
    }
}
