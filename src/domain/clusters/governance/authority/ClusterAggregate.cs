namespace Whycespace.Domain.Core.Cluster.Aggregates;

public sealed class ClusterAggregate
{
    private readonly List<SubCluster> _subClusters = new();

    public Guid ClusterId { get; }

    public string ClusterName { get; }

    public IReadOnlyCollection<SubCluster> SubClusters => _subClusters.AsReadOnly();

    public ClusterAggregate(Guid clusterId, string clusterName)
    {
        ClusterId = clusterId;
        ClusterName = clusterName;
    }

    internal void AddSubCluster(SubCluster subCluster)
    {
        _subClusters.Add(subCluster);
    }
}
