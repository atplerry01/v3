namespace Whycespace.Systems.Downstream.Clusters.Providers;

public sealed class ClusterProviderRegistry
{
    private readonly Dictionary<Guid, ClusterProviderRecord> _providers = new();
    private readonly Dictionary<string, List<Guid>> _clusterIndex = new();

    public void Register(ClusterProviderRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);

        if (_providers.ContainsKey(record.ProviderId))
            throw new InvalidOperationException($"Provider '{record.ProviderId}' is already registered.");

        _providers[record.ProviderId] = record;

        if (!_clusterIndex.TryGetValue(record.ClusterId, out var list))
        {
            list = new List<Guid>();
            _clusterIndex[record.ClusterId] = list;
        }
        list.Add(record.ProviderId);
    }

    public ClusterProviderRecord? Get(Guid providerId)
    {
        _providers.TryGetValue(providerId, out var record);
        return record;
    }

    public IReadOnlyList<ClusterProviderRecord> GetByCluster(string clusterId)
    {
        if (!_clusterIndex.TryGetValue(clusterId, out var ids))
            return [];

        return ids.Select(id => _providers[id]).ToList();
    }

    public IReadOnlyList<ClusterProviderRecord> ListAll() => _providers.Values.ToList();
}
