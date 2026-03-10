namespace Whycespace.Domain.Cluster;

public sealed record Cluster(
    Guid ClusterId,
    string Name,
    string Region,
    ClusterType Type,
    ClusterStatus Status,
    ClusterProvider Provider,
    DateTimeOffset RegisteredAt
);

public enum ClusterType
{
    Mobility,
    Property,
    Economic,
    Mixed
}

public enum ClusterStatus
{
    Active,
    Suspended,
    Decommissioned
}
