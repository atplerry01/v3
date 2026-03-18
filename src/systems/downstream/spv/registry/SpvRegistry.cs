namespace Whycespace.Systems.Downstream.Spv.Registry;

public sealed class SpvRegistry
{
    private readonly Dictionary<Guid, SpvRegistryRecord> _spvs = new();
    private readonly Dictionary<string, List<Guid>> _clusterIndex = new();

    public void Register(SpvRegistryRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);

        if (_spvs.ContainsKey(record.SpvId))
            throw new InvalidOperationException($"SPV '{record.SpvId}' is already registered.");

        _spvs[record.SpvId] = record;

        if (!_clusterIndex.TryGetValue(record.ClusterId, out var list))
        {
            list = new List<Guid>();
            _clusterIndex[record.ClusterId] = list;
        }
        list.Add(record.SpvId);
    }

    public SpvRegistryRecord? Get(Guid spvId)
    {
        _spvs.TryGetValue(spvId, out var record);
        return record;
    }

    public IReadOnlyList<SpvRegistryRecord> GetByCluster(string clusterId)
    {
        if (!_clusterIndex.TryGetValue(clusterId, out var ids))
            return [];

        return ids.Select(id => _spvs[id]).ToList();
    }

    public IReadOnlyList<SpvRegistryRecord> ListAll() => _spvs.Values.ToList();
}
