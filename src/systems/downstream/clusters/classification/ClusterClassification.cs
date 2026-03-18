namespace Whycespace.Systems.Downstream.Clusters.Classification;

public sealed record ClusterClassification(
    string ClusterId,
    string Sector,
    string RiskTier,
    string OperationalClass,
    IReadOnlyList<string> Tags
)
{
    public static ClusterClassification Default(string clusterId, string sector) => new(
        clusterId,
        sector,
        RiskTier: "Standard",
        OperationalClass: "General",
        Tags: []
    );
}
