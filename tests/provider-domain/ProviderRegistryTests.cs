namespace Whycespace.ProviderDomain.Tests;

using Whycespace.ClusterDomain;

public sealed class ProviderRegistryTests
{
    [Fact]
    public void RegisterProvider_IsRetrievableById()
    {
        var registry = new ClusterProviderRegistry();
        var clusterId = Guid.NewGuid();

        var provider = registry.RegisterProvider("DriverProvider", "DriverProvider", clusterId);

        var retrieved = registry.GetProvider(provider.ProviderId);
        Assert.NotNull(retrieved);
        Assert.Equal("DriverProvider", retrieved.ProviderName);
    }

    [Fact]
    public void GetProvider_UnknownId_ReturnsNull()
    {
        var registry = new ClusterProviderRegistry();

        Assert.Null(registry.GetProvider(Guid.NewGuid()));
    }
}
