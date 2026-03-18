using Whycespace.Systems.Downstream.Clusters.Definition;

namespace Whycespace.Systems.Downstream.Clusters.Implementations.WhyceMobility;

public static class WhyceMobilitySubClusters
{
    public static SubClusterDefinition Taxi() => new(
        "whyce-mobility-taxi",
        "Taxi",
        "whyce-mobility",
        "RideHailing"
    );

    public static SubClusterDefinition Logistics() => new(
        "whyce-mobility-logistics",
        "Logistics",
        "whyce-mobility",
        "FreightTransport"
    );

    public static SubClusterDefinition Fleet() => new(
        "whyce-mobility-fleet",
        "Fleet",
        "whyce-mobility",
        "FleetManagement"
    );
}
