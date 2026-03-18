namespace Whycespace.Domain.Economic.Spv;

public sealed class SpvEconomicEntity
{
    public Guid SpvId { get; }

    public string ClusterName { get; }

    public string SubClusterName { get; }

    public DateTime CreatedAt { get; }

    public SpvEconomicEntity(Guid spvId, string cluster, string subCluster)
    {
        SpvId = spvId;
        ClusterName = cluster;
        SubClusterName = subCluster;
        CreatedAt = DateTime.UtcNow;
    }
}
