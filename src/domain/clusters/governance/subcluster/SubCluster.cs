namespace Whycespace.Domain.Clusters.Governance.Subcluster;

public sealed class SubCluster
{
    public Guid SubClusterId { get; }

    public string SubClusterName { get; }

    public Guid ParentClusterId { get; }

    public SubCluster(Guid subClusterId, string subClusterName, Guid parentClusterId)
    {
        SubClusterId = subClusterId;
        SubClusterName = subClusterName;
        ParentClusterId = parentClusterId;
    }
}
