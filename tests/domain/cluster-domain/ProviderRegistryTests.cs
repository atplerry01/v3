namespace Whycespace.ClusterDomain.Tests;

using Whycespace.Domain.Clusters.Governance.Provider;

public sealed class ProviderRegistryTests
{
    [Fact]
    public void RegisterProvider_IsRetrievable()
    {
        var registry = new ClusterProviderRegistry();
        var clusterId = Guid.NewGuid();

        var provider = registry.RegisterProvider("DriverProvider", "DriverProvider", clusterId);

        Assert.NotEqual(Guid.Empty, provider.ProviderId);
        Assert.Equal("DriverProvider", provider.ProviderName);
        Assert.Equal(clusterId, provider.ClusterId);
        Assert.Single(registry.GetProviders());
    }

    [Fact]
    public void GetProvidersByCluster_FiltersCorrectly()
    {
        var registry = new ClusterProviderRegistry();
        var cluster1 = Guid.NewGuid();
        var cluster2 = Guid.NewGuid();

        registry.RegisterProvider("DriverProvider", "DriverProvider", cluster1);
        registry.RegisterProvider("VehicleProvider", "VehicleProvider", cluster1);
        registry.RegisterProvider("PropertyManager", "PropertyManagerProvider", cluster2);

        Assert.Equal(2, registry.GetProvidersByCluster(cluster1).Count);
        Assert.Single(registry.GetProvidersByCluster(cluster2));
    }
}
