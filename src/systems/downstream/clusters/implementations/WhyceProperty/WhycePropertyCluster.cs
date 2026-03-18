using Whycespace.Systems.Downstream.Clusters.Definition;

namespace Whycespace.Systems.Downstream.Clusters.Implementations.WhyceProperty;

public static class WhycePropertyCluster
{
    public static ClusterDefinition Create() => new(
        "whyce-property",
        "WhyceProperty",
        "RealEstate",
        new[] { "letting", "sales", "management" },
        DateTimeOffset.UtcNow
    );
}
