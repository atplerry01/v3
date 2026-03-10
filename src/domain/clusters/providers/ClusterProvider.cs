namespace Whycespace.ClusterDomain;

public sealed class ClusterProvider
{
    public Guid ProviderId { get; }

    public string ProviderName { get; }

    public Guid ClusterId { get; }

    public ClusterProvider(Guid providerId, string providerName, Guid clusterId)
    {
        ProviderId = providerId;
        ProviderName = providerName;
        ClusterId = clusterId;
    }
}
