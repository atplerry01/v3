namespace Whycespace.Systems.Downstream.Coordination;

public sealed class ClusterExecutionRouter
{
    private readonly HashSet<string> _registeredClusters = new();
    private readonly HashSet<string> _registeredSubClusters = new();

    public void RegisterCluster(string clusterId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(clusterId);
        _registeredClusters.Add(clusterId);
    }

    public void RegisterSubCluster(string subClusterId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(subClusterId);
        _registeredSubClusters.Add(subClusterId);
    }

    public bool ResolveCluster(string clusterId, string subClusterId)
    {
        if (!_registeredClusters.Contains(clusterId))
            return false;

        if (!string.IsNullOrWhiteSpace(subClusterId) && !_registeredSubClusters.Contains(subClusterId))
            return false;

        return true;
    }

    public IReadOnlyCollection<string> GetRegisteredClusters() => _registeredClusters;
}
