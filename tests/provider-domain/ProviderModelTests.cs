namespace Whycespace.ProviderDomain.Tests;

using Whycespace.Domain.Clusters;
using Whycespace.Domain.Core.Providers;

public sealed class ProviderModelTests
{
    [Fact]
    public void CreateProvider_AssignsAllFields()
    {
        var id = Guid.NewGuid();
        var clusterId = Guid.NewGuid();

        var provider = new ClusterProvider(id, "DriverProvider", "DriverProvider", clusterId);

        Assert.Equal(id, provider.ProviderId);
        Assert.Equal("DriverProvider", provider.ProviderName);
        Assert.Equal("DriverProvider", provider.ProviderType);
        Assert.Equal(clusterId, provider.ClusterId);
    }
}
