namespace Whycespace.ClusterDomain.Tests;

using Whycespace.Domain.Clusters.Governance.Registry;
using Whycespace.Systems.Downstream.Spv.Registry;

public sealed class SpvRegistryTests
{
    [Fact]
    public void RegisterSpv_IsRetrievable()
    {
        var registry = new SpvSubClusterRegistry();
        var spvId = Guid.NewGuid();

        registry.Register(spvId, "Taxi");

        Assert.Equal("Taxi", registry.GetSubCluster(spvId));
    }

    [Fact]
    public void GetSubCluster_UnknownSpv_ReturnsNull()
    {
        var registry = new SpvSubClusterRegistry();

        Assert.Null(registry.GetSubCluster(Guid.NewGuid()));
    }
}
