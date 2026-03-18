namespace Whycespace.Systems.Downstream.Clusters.Providers;

public sealed class ClusterProviderPolicy
{
    public bool CanRegister(Guid identityId, string clusterId, ClusterProviderType providerType)
    {
        if (identityId == Guid.Empty)
            return false;

        if (string.IsNullOrWhiteSpace(clusterId))
            return false;

        return true;
    }

    public bool RequiresLicense(ClusterProviderType providerType)
    {
        return providerType switch
        {
            ClusterProviderType.Individual => true,
            ClusterProviderType.Enterprise => true,
            ClusterProviderType.Fleet => true,
            ClusterProviderType.Agency => true,
            ClusterProviderType.Government => false,
            _ => true
        };
    }
}
