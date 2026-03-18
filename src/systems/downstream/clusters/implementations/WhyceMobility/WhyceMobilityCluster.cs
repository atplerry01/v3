using Whycespace.Systems.Downstream.Clusters.Definition;

namespace Whycespace.Systems.Downstream.Clusters.Implementations.WhyceMobility;

public static class WhyceMobilityCluster
{
    public static ClusterDefinition Create() => new(
        "whyce-mobility",
        "WhyceMobility",
        "Transportation",
        new[] { "taxi", "logistics", "fleet" },
        DateTimeOffset.UtcNow
    );
}
