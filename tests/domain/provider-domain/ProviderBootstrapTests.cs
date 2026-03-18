namespace Whycespace.ProviderDomain.Tests;

using Whycespace.Domain.Clusters.Governance.Administration;
using Whycespace.Domain.Clusters.Governance.Lifecycle;
using Whycespace.Domain.Clusters.Governance.Provider;
using Whycespace.Domain.Clusters.Governance.Registry;
using Whycespace.Systems.Downstream.Spv.Registry;

public sealed class ProviderBootstrapTests
{
    [Fact]
    public void Bootstrap_AssignsProvidersToSubClusters()
    {
        var admin = new ClusterAdministrationService();
        var registry = new ClusterProviderRegistry();
        var spvRegistry = new SpvSubClusterRegistry();
        var assignmentService = new ProviderAssignmentService();
        var bootstrapper = new ClusterBootstrapper(admin, registry, spvRegistry, assignmentService);

        bootstrapper.Bootstrap();

        var taxiProviders = assignmentService.GetProvidersForSubCluster("Taxi");
        Assert.Equal(2, taxiProviders.Count);

        var lettingProviders = assignmentService.GetProvidersForSubCluster("LettingAgent");
        Assert.Equal(2, lettingProviders.Count);
    }
}
