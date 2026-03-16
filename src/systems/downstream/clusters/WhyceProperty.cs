namespace Whycespace.Systems.Downstream.Clusters;

public static class WhyceProperty
{
    public static ClusterDefinition CreateCluster() => new(
        "whyce-property",
        "WhyceProperty",
        "RealEstate",
        new[] { "letting", "sales", "management" },
        DateTimeOffset.UtcNow
    );

    public static SubClusterDefinition PropertyLettingSubCluster() => new(
        "whyce-property-letting",
        "PropertyLetting",
        "whyce-property",
        "PropertyLetting"
    );
}
