using Whycespace.Systems.Downstream.Clusters.Definition;

namespace Whycespace.Systems.Downstream.Clusters.Implementations.WhyceProperty;

public static class WhycePropertySubClusters
{
    public static SubClusterDefinition Letting() => new(
        "whyce-property-letting",
        "PropertyLetting",
        "whyce-property",
        "PropertyLetting"
    );

    public static SubClusterDefinition Sales() => new(
        "whyce-property-sales",
        "PropertySales",
        "whyce-property",
        "PropertySales"
    );

    public static SubClusterDefinition Management() => new(
        "whyce-property-management",
        "PropertyManagement",
        "whyce-property",
        "PropertyManagement"
    );
}
