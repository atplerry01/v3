using Whycespace.Systems.Downstream.Clusters.Definition;

namespace Whycespace.Systems.Downstream.Clusters.Registry;

public sealed class ClusterRegistry
{
    private readonly Dictionary<string, ClusterDefinition> _clusters = new();
    private readonly Dictionary<string, SubClusterDefinition> _subClusters = new();

    public void RegisterCluster(ClusterDefinition cluster)
    {
        ArgumentNullException.ThrowIfNull(cluster);
        ArgumentException.ThrowIfNullOrWhiteSpace(cluster.ClusterId);

        if (_clusters.ContainsKey(cluster.ClusterId))
            throw new InvalidOperationException($"Cluster '{cluster.ClusterId}' is already registered.");

        _clusters[cluster.ClusterId] = cluster;
    }

    public void RegisterSubCluster(SubClusterDefinition subCluster)
    {
        ArgumentNullException.ThrowIfNull(subCluster);
        ArgumentException.ThrowIfNullOrWhiteSpace(subCluster.SubClusterId);

        if (_subClusters.ContainsKey(subCluster.SubClusterId))
            throw new InvalidOperationException($"SubCluster '{subCluster.SubClusterId}' is already registered.");

        _subClusters[subCluster.SubClusterId] = subCluster;
    }

    public ClusterDefinition? GetCluster(string clusterId)
    {
        _clusters.TryGetValue(clusterId, out var cluster);
        return cluster;
    }

    public IReadOnlyList<ClusterDefinition> GetAllClusters() => _clusters.Values.ToList();

    public IReadOnlyList<SubClusterDefinition> GetSubClusters(string clusterId)
        => _subClusters.Values.Where(sc => sc.ClusterId == clusterId).ToList();
}
