namespace Whycespace.Domain.Clusters.Governance.Lifecycle;

using Whycespace.Domain.Clusters.Governance.Administration;
using Whycespace.Domain.Clusters.Governance.Provider;
using Whycespace.Domain.Clusters.Governance.Registry;

public sealed class ClusterBootstrapper
{
    private readonly ClusterAdministrationService _administration;
    private readonly ClusterProviderRegistry _providerRegistry;
    private readonly ISpvRegistry _spvRegistry;
    private readonly ProviderAssignmentService _assignmentService;
    private readonly ProviderBootstrapper _providerBootstrapper;

    public ClusterBootstrapper(
        ClusterAdministrationService administration,
        ClusterProviderRegistry providerRegistry,
        ISpvRegistry spvRegistry,
        ProviderAssignmentService assignmentService)
    {
        _administration = administration;
        _providerRegistry = providerRegistry;
        _spvRegistry = spvRegistry;
        _assignmentService = assignmentService;
        _providerBootstrapper = new ProviderBootstrapper(providerRegistry, administration, assignmentService);
    }

    public void Bootstrap()
    {
        var mobility = _administration.RegisterCluster("WhyceMobility");
        _administration.AddSubCluster(mobility.ClusterId, "Taxi");

        _providerRegistry.RegisterProvider("DriverProvider", "DriverProvider", mobility.ClusterId);
        _providerRegistry.RegisterProvider("VehicleProvider", "VehicleProvider", mobility.ClusterId);

        var property = _administration.RegisterCluster("WhyceProperty");
        _administration.AddSubCluster(property.ClusterId, "LettingAgent");

        _providerRegistry.RegisterProvider("PropertyManagerProvider", "PropertyManagerProvider", property.ClusterId);
        _providerRegistry.RegisterProvider("MaintenanceProvider", "MaintenanceProvider", property.ClusterId);

        _providerBootstrapper.Bootstrap();
    }

    public ClusterAdministrationService Administration => _administration;
    public ClusterProviderRegistry ProviderRegistry => _providerRegistry;
    public ISpvRegistry SpvRegistry => _spvRegistry;
    public ProviderAssignmentService AssignmentService => _assignmentService;
}
