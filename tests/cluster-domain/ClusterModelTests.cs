namespace Whycespace.ClusterDomain.Tests;
using Whycespace.Domain.Clusters;

public sealed class ClusterModelTests
{
    [Fact]
    public void CreateCluster_AssignsIdAndName()
    {
        var service = new ClusterAdministrationService();

        var cluster = service.RegisterCluster("WhyceMobility");

        Assert.NotEqual(Guid.Empty, cluster.ClusterId);
        Assert.Equal("WhyceMobility", cluster.ClusterName);
        Assert.Empty(cluster.SubClusters);
    }

    [Fact]
    public void AddSubCluster_AttachesToCluster()
    {
        var service = new ClusterAdministrationService();
        var cluster = service.RegisterCluster("WhyceMobility");

        var sub = service.AddSubCluster(cluster.ClusterId, "Taxi");

        Assert.Single(cluster.SubClusters);
        Assert.Equal("Taxi", sub.SubClusterName);
        Assert.Equal(cluster.ClusterId, sub.ParentClusterId);
    }
}
