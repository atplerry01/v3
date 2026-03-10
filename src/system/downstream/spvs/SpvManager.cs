namespace Whycespace.System.Downstream.Spvs;

public sealed record SpvRegistration(
    Guid SpvId,
    string Name,
    string ClusterId,
    decimal AllocatedCapital,
    DateTimeOffset CreatedAt
);

public sealed class SpvManager
{
    private readonly Dictionary<Guid, SpvRegistration> _spvs = new();

    public SpvRegistration Register(string name, string clusterId, decimal allocatedCapital)
    {
        var spv = new SpvRegistration(Guid.NewGuid(), name, clusterId, allocatedCapital, DateTimeOffset.UtcNow);
        _spvs[spv.SpvId] = spv;
        return spv;
    }

    public SpvRegistration? Get(Guid spvId)
    {
        _spvs.TryGetValue(spvId, out var spv);
        return spv;
    }

    public IReadOnlyList<SpvRegistration> GetByCluster(string clusterId)
        => _spvs.Values.Where(s => s.ClusterId == clusterId).ToList();
}
