namespace Whycespace.Domain.Clusters.Governance.Provider;

public sealed class ClusterProvider
{
    public Guid ProviderId { get; }

    public string ProviderName { get; }

    public string ProviderType { get; }

    public Guid ClusterId { get; }

    public ClusterProvider(Guid providerId, string providerName, string providerType, Guid clusterId)
    {
        ProviderId = providerId;
        ProviderName = providerName;
        ProviderType = providerType;
        ClusterId = clusterId;
    }
}
