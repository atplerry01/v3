namespace Whycespace.ProviderDomain.Tests;

using Whycespace.ClusterDomain;

public sealed class ProviderAssignmentTests
{
    [Fact]
    public void AssignProvider_IsRetrievable()
    {
        var service = new ProviderAssignmentService();
        var providerId = Guid.NewGuid();

        service.AssignProviderToSubCluster(providerId, "Taxi");

        var providers = service.GetProvidersForSubCluster("Taxi");
        Assert.Single(providers);
        Assert.Equal(providerId, providers[0]);
    }

    [Fact]
    public void GetProvidersForSubCluster_UnknownSubCluster_ReturnsEmpty()
    {
        var service = new ProviderAssignmentService();

        var providers = service.GetProvidersForSubCluster("Unknown");

        Assert.Empty(providers);
    }
}
