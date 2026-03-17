namespace Whycespace.ClusterTemplatePlatform;

using Whycespace.Domain.Core.Cluster.Services;
using Whycespace.Domain.Core.Providers;

public sealed class ClusterTemplateService
{
    public ClusterTemplateRegistry Registry { get; }
    public ClusterTemplateGenerator Generator { get; }

    public ClusterTemplateService(
        ClusterAdministrationService administration,
        ClusterProviderRegistry providerRegistry,
        ProviderAssignmentService assignmentService)
    {
        Registry = new ClusterTemplateRegistry();
        Generator = new ClusterTemplateGenerator(Registry, administration, providerRegistry, assignmentService);

        RegisterPilotTemplates();
    }

    private void RegisterPilotTemplates()
    {
        Registry.RegisterTemplate(new ClusterTemplate(
            "MobilityTemplate",
            "WhyceMobility",
            new[]
            {
                new SubClusterTemplate("Taxi", new[] { "DriverProvider", "VehicleProvider" })
            },
            new[] { "DriverProvider", "VehicleProvider" }));

        Registry.RegisterTemplate(new ClusterTemplate(
            "PropertyTemplate",
            "WhyceProperty",
            new[]
            {
                new SubClusterTemplate("LettingAgent", new[] { "PropertyManagerProvider", "MaintenanceProvider" })
            },
            new[] { "PropertyManagerProvider", "MaintenanceProvider" }));
    }
}
