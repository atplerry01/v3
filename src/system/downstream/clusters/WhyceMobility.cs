namespace Whycespace.System.Downstream.Clusters;

public static class WhyceMobility
{
    public static ClusterDefinition CreateCluster() => new(
        "whyce-mobility",
        "WhyceMobility",
        "Transportation",
        new[] { "taxi", "logistics", "fleet" },
        DateTimeOffset.UtcNow
    );

    public static SubClusterDefinition TaxiSubCluster() => new(
        "whyce-mobility-taxi",
        "Taxi",
        "whyce-mobility",
        "RideHailing"
    );
}
