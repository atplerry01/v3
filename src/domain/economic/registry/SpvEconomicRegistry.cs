namespace Whycespace.EconomicDomain;

public sealed class SpvEconomicRegistry
{
    private readonly Dictionary<Guid, SpvEconomicEntity> _spvs = new();

    public SpvEconomicEntity RegisterSpv(string clusterName, string subClusterName)
    {
        var spv = new SpvEconomicEntity(Guid.NewGuid(), clusterName, subClusterName);
        _spvs[spv.SpvId] = spv;
        return spv;
    }

    public SpvEconomicEntity? GetSpv(Guid spvId)
    {
        return _spvs.GetValueOrDefault(spvId);
    }

    public IReadOnlyCollection<SpvEconomicEntity> ListSpvs()
    {
        return _spvs.Values.ToList().AsReadOnly();
    }
}
