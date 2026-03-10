namespace Whycespace.System.Downstream.Clusters;

public sealed class ClusterRegistry
{
    private readonly Dictionary<string, ClusterDefinition> _clusters = new();
    private readonly Dictionary<string, SubClusterDefinition> _subClusters = new();

    public void RegisterCluster(ClusterDefinition cluster)
    {
        _clusters[cluster.ClusterId] = cluster;
    }

    public void RegisterSubCluster(SubClusterDefinition subCluster)
    {
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
