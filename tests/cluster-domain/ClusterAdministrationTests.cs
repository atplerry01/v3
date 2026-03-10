namespace Whycespace.ClusterDomain.Tests;

public sealed class ClusterAdministrationTests
{
    [Fact]
    public void RegisterCluster_IsRetrievable()
    {
        var service = new ClusterAdministrationService();

        var cluster = service.RegisterCluster("WhyceProperty");

        var retrieved = service.GetCluster(cluster.ClusterId);
        Assert.NotNull(retrieved);
        Assert.Equal("WhyceProperty", retrieved.ClusterName);
    }

    [Fact]
    public void GetCluster_UnknownId_ReturnsNull()
    {
        var service = new ClusterAdministrationService();

        var result = service.GetCluster(Guid.NewGuid());

        Assert.Null(result);
    }

    [Fact]
    public void AddSubCluster_UnknownCluster_Throws()
    {
        var service = new ClusterAdministrationService();

        Assert.Throws<InvalidOperationException>(() =>
            service.AddSubCluster(Guid.NewGuid(), "Taxi"));
    }
}
