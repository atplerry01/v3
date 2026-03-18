namespace Whycespace.Systems.Downstream.Spv.Registry;

using Whycespace.Domain.Clusters.Governance.Registry;

public sealed class SpvSubClusterRegistry : ISpvRegistry
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
