namespace Whycespace.Systems.Downstream.Clusters;

public sealed record ClusterDefinition(
    string ClusterId,
    string Name,
    string Sector,
    IReadOnlyList<string> SubClusters,
    DateTimeOffset CreatedAt
);

public sealed record SubClusterDefinition(
    string SubClusterId,
    string Name,
    string ClusterId,
    string ServiceType
);
