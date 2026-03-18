namespace Whycespace.Domain.Clusters.Governance.Provider;

using Whycespace.Domain.Clusters.Governance.Administration;
using Whycespace.Domain.Clusters.Governance.Authority;
using Whycespace.Domain.Clusters.Governance.Lifecycle;
using Whycespace.Domain.Clusters.Governance.Subcluster;

public sealed class ProviderBootstrapper
{
    private readonly ClusterProviderRegistry _providerRegistry;
    private readonly ClusterAdministrationService _administration;
    private readonly ProviderAssignmentService _assignmentService;

    public ProviderBootstrapper(
        ClusterProviderRegistry providerRegistry,
        ClusterAdministrationService administration,
        ProviderAssignmentService assignmentService)
    {
        _providerRegistry = providerRegistry;
        _administration = administration;
        _assignmentService = assignmentService;
    }

    public void Bootstrap()
    {
        var clusters = _administration.GetAllClusters();

        foreach (var cluster in clusters)
        {
            switch (cluster.ClusterName)
            {
                case "WhyceMobility":
                    BootstrapMobility(cluster);
                    break;
                case "WhyceProperty":
                    BootstrapProperty(cluster);
                    break;
            }
        }
    }

    private void BootstrapMobility(ClusterAggregate cluster)
    {
        var taxi = cluster.SubClusters.FirstOrDefault(s => s.SubClusterName == "Taxi");
        if (taxi is null) return;

        var providers = _providerRegistry.GetProvidersByCluster(cluster.ClusterId);
        foreach (var provider in providers)
        {
            _assignmentService.AssignProviderToSubCluster(provider.ProviderId, taxi.SubClusterName);
        }
    }

    private void BootstrapProperty(ClusterAggregate cluster)
    {
        var letting = cluster.SubClusters.FirstOrDefault(s => s.SubClusterName == "LettingAgent");
        if (letting is null) return;

        var providers = _providerRegistry.GetProvidersByCluster(cluster.ClusterId);
        foreach (var provider in providers)
        {
            _assignmentService.AssignProviderToSubCluster(provider.ProviderId, letting.SubClusterName);
        }
    }

    public ProviderAssignmentService AssignmentService => _assignmentService;
}
