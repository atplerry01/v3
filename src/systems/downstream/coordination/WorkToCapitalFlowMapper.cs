namespace Whycespace.Systems.Downstream.Coordination;

public sealed class WorkToCapitalFlowMapper
{
    private readonly List<CapitalFlowRecord> _flows = new();

    public void RecordFlow(string clusterId, Guid spvId, decimal amount)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(clusterId);

        var record = new CapitalFlowRecord(
            Guid.NewGuid(),
            clusterId,
            spvId,
            amount,
            DateTimeOffset.UtcNow
        );

        _flows.Add(record);
    }

    public IReadOnlyList<CapitalFlowRecord> GetFlowsByCluster(string clusterId)
        => _flows.Where(f => f.ClusterId == clusterId).ToList();

    public IReadOnlyList<CapitalFlowRecord> GetFlowsBySpv(Guid spvId)
        => _flows.Where(f => f.SpvId == spvId).ToList();

    public decimal GetTotalFlowForSpv(Guid spvId)
        => _flows.Where(f => f.SpvId == spvId).Sum(f => f.Amount);
}

public sealed record CapitalFlowRecord(
    Guid FlowId,
    string ClusterId,
    Guid SpvId,
    decimal Amount,
    DateTimeOffset Timestamp
);
