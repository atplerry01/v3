namespace Whycespace.ClusterDomain;

public sealed class SpvRegistry
{
    private readonly Dictionary<Guid, string> _spvs = new();

    public void Register(Guid spvId, string subCluster)
    {
        _spvs[spvId] = subCluster;
    }

    public string? GetSubCluster(Guid spvId)
    {
        return _spvs.GetValueOrDefault(spvId);
    }

    public IReadOnlyDictionary<Guid, string> GetAll()
    {
        return _spvs.AsReadOnly();
    }
}
