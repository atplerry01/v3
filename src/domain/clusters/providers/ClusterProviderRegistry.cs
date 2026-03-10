namespace Whycespace.ClusterDomain;

public sealed class ClusterProviderRegistry
{
    private readonly List<ClusterProvider> _providers = new();

    public ClusterProvider RegisterProvider(string providerName, string providerType, Guid clusterId)
    {
        var provider = new ClusterProvider(Guid.NewGuid(), providerName, providerType, clusterId);
        _providers.Add(provider);
        return provider;
    }

    public ClusterProvider? GetProvider(Guid providerId)
    {
        return _providers.FirstOrDefault(p => p.ProviderId == providerId);
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
