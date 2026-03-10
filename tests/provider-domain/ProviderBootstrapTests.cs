namespace Whycespace.ProviderDomain.Tests;

using Whycespace.ClusterDomain;

public sealed class ProviderBootstrapTests
{
    [Fact]
    public void Bootstrap_AssignsProvidersToSubClusters()
    {
        var admin = new ClusterAdministrationService();
        var registry = new ClusterProviderRegistry();
        var spvRegistry = new SpvRegistry();
        var assignmentService = new ProviderAssignmentService();
        var bootstrapper = new ClusterBootstrapper(admin, registry, spvRegistry, assignmentService);

        bootstrapper.Bootstrap();

        var taxiProviders = assignmentService.GetProvidersForSubCluster("Taxi");
        Assert.Equal(2, taxiProviders.Count);

        var lettingProviders = assignmentService.GetProvidersForSubCluster("LettingAgent");
        Assert.Equal(2, lettingProviders.Count);
    }
}
