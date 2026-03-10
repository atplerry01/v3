namespace Whycespace.ClusterDomain.Tests;

public sealed class SpvRegistryTests
{
    [Fact]
    public void RegisterSpv_IsRetrievable()
    {
        var registry = new SpvRegistry();
        var spvId = Guid.NewGuid();

        registry.Register(spvId, "Taxi");

        Assert.Equal("Taxi", registry.GetSubCluster(spvId));
    }

    [Fact]
    public void GetSubCluster_UnknownSpv_ReturnsNull()
    {
        var registry = new SpvRegistry();

        Assert.Null(registry.GetSubCluster(Guid.NewGuid()));
    }
}
