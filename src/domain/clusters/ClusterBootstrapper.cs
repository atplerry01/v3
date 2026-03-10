namespace Whycespace.ClusterDomain;

public sealed class ClusterBootstrapper
{
    private readonly ClusterAdministrationService _administration;
    private readonly ClusterProviderRegistry _providerRegistry;
    private readonly SpvRegistry _spvRegistry;

    public ClusterBootstrapper(
        ClusterAdministrationService administration,
        ClusterProviderRegistry providerRegistry,
        SpvRegistry spvRegistry)
    {
        _administration = administration;
        _providerRegistry = providerRegistry;
        _spvRegistry = spvRegistry;
    }

    public void Bootstrap()
    {
        var mobility = _administration.RegisterCluster("WhyceMobility");
        _administration.AddSubCluster(mobility.ClusterId, "Taxi");

        _providerRegistry.RegisterProvider("DriverProvider", mobility.ClusterId);
        _providerRegistry.RegisterProvider("VehicleProvider", mobility.ClusterId);

        var property = _administration.RegisterCluster("WhyceProperty");
        _administration.AddSubCluster(property.ClusterId, "LettingAgent");

        _providerRegistry.RegisterProvider("PropertyManager", property.ClusterId);
        _providerRegistry.RegisterProvider("MaintenanceProvider", property.ClusterId);
    }

    public ClusterAdministrationService Administration => _administration;
    public ClusterProviderRegistry ProviderRegistry => _providerRegistry;
    public SpvRegistry SpvRegistry => _spvRegistry;
}
