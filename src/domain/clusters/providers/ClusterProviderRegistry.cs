namespace Whycespace.ClusterDomain;

public sealed class ClusterProviderRegistry
{
    private readonly List<ClusterProvider> _providers = new();

    public ClusterProvider RegisterProvider(string providerName, Guid clusterId)
    {
        var provider = new ClusterProvider(Guid.NewGuid(), providerName, clusterId);
        _providers.Add(provider);
        return provider;
    }

    public IReadOnlyCollection<ClusterProvider> GetProviders()
    {
        return _providers.AsReadOnly();
    }

    public IReadOnlyCollection<ClusterProvider> GetProvidersByCluster(Guid clusterId)
    {
        return _providers.Where(p => p.ClusterId == clusterId).ToList().AsReadOnly();
    }
}
