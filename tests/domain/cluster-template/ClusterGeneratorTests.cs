namespace Whycespace.ClusterTemplatePlatform.Tests;

using Whycespace.Domain.Clusters;
using Whycespace.Domain.Clusters.Governance.Administration;
using Whycespace.Domain.Clusters.Governance.Lifecycle;
using Whycespace.Domain.Clusters.Governance.Provider;

public sealed class ClusterGeneratorTests
{
    [Fact]
    public void GenerateCluster_CreatesClusterWithSubClusters()
    {
        var administration = new ClusterAdministrationService();
        var providerRegistry = new ClusterProviderRegistry();
        var assignmentService = new ProviderAssignmentService();

        var templateRegistry = new ClusterTemplateRegistry();
        templateRegistry.RegisterTemplate(new ClusterTemplate(
            "MobilityTemplate",
            "WhyceMobility",
            new[]
            {
                new SubClusterTemplate("Taxi", new[] { "DriverProvider", "VehicleProvider" })
            },
            new[] { "DriverProvider", "VehicleProvider" }));

        var generator = new ClusterTemplateGenerator(
            templateRegistry, administration, providerRegistry, assignmentService);

        var result = generator.GenerateCluster("MobilityTemplate");

        Assert.Equal("WhyceMobility", result.Cluster);
        Assert.Contains("Taxi", result.SubClusters);
    }

    [Fact]
    public void GenerateCluster_RegistersProviders()
    {
        var administration = new ClusterAdministrationService();
        var providerRegistry = new ClusterProviderRegistry();
        var assignmentService = new ProviderAssignmentService();

        var templateRegistry = new ClusterTemplateRegistry();
        templateRegistry.RegisterTemplate(new ClusterTemplate(
            "PropertyTemplate",
            "WhyceProperty",
            new[]
            {
                new SubClusterTemplate("LettingAgent", new[] { "PropertyManagerProvider", "MaintenanceProvider" })
            },
            new[] { "PropertyManagerProvider", "MaintenanceProvider" }));

        var generator = new ClusterTemplateGenerator(
            templateRegistry, administration, providerRegistry, assignmentService);

        generator.GenerateCluster("PropertyTemplate");

        var providers = providerRegistry.GetProviders();
        Assert.Contains(providers, p => p.ProviderName == "PropertyManagerProvider");
        Assert.Contains(providers, p => p.ProviderName == "MaintenanceProvider");
    }

    [Fact]
    public void GenerateCluster_AssignsProvidersToSubClusters()
    {
        var administration = new ClusterAdministrationService();
        var providerRegistry = new ClusterProviderRegistry();
        var assignmentService = new ProviderAssignmentService();

        var templateRegistry = new ClusterTemplateRegistry();
        templateRegistry.RegisterTemplate(new ClusterTemplate(
            "MobilityTemplate",
            "WhyceMobility",
            new[]
            {
                new SubClusterTemplate("Taxi", new[] { "DriverProvider", "VehicleProvider" })
            },
            new[] { "DriverProvider", "VehicleProvider" }));

        var generator = new ClusterTemplateGenerator(
            templateRegistry, administration, providerRegistry, assignmentService);

        generator.GenerateCluster("MobilityTemplate");

        var assignments = assignmentService.GetProvidersForSubCluster("Taxi");
        Assert.Equal(2, assignments.Count);
    }
}
