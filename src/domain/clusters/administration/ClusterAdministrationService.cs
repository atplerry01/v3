namespace Whycespace.Domain.Clusters;

public sealed class ClusterAdministrationService
{
    private readonly Dictionary<Guid, Cluster> _clusters = new();

    public Cluster RegisterCluster(string clusterName)
    {
        var cluster = new Cluster(Guid.NewGuid(), clusterName);
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

    public Cluster? GetCluster(Guid clusterId)
    {
        return _clusters.GetValueOrDefault(clusterId);
    }

    public IReadOnlyCollection<Cluster> GetAllClusters()
    {
        return _clusters.Values.ToList().AsReadOnly();
    }
}
