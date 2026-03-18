namespace Whycespace.Domain.Clusters.Governance.Administration;

using Whycespace.Domain.Clusters.Governance.Authority;
using Whycespace.Domain.Clusters.Governance.Subcluster;

public sealed class ClusterAdministrationService
{
    private readonly Dictionary<Guid, ClusterAggregate> _clusters = new();

    public ClusterAggregate RegisterCluster(string clusterName)
    {
        var cluster = new ClusterAggregate(Guid.NewGuid(), clusterName);
        _clusters[cluster.ClusterId] = cluster;
        return cluster;
    }

    public SubCluster AddSubCluster(Guid clusterId, string subClusterName)
    {
        if (!_clusters.TryGetValue(clusterId, out var cluster))
            throw new InvalidOperationException($"Cluster {clusterId} not found.");

        var subCluster = new SubCluster(Guid.NewGuid(), subClusterName, clusterId);
        cluster.AddSubCluster(subCluster);
        return subCluster;
    }

    public ClusterAggregate? GetCluster(Guid clusterId)
    {
        return _clusters.GetValueOrDefault(clusterId);
    }

    public IReadOnlyCollection<ClusterAggregate> GetAllClusters()
    {
        return _clusters.Values.ToList().AsReadOnly();
    }
}
