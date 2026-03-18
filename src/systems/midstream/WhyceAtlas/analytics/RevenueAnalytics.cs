namespace Whycespace.Systems.Midstream.WhyceAtlas.Analytics;

public sealed class RevenueAnalytics
{
    private readonly List<RevenueSnapshot> _snapshots = new();

    public void RecordSnapshot(RevenueSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        _snapshots.Add(snapshot);
    }

    public decimal GetTotalRevenue(string? clusterId = null)
    {
        var filtered = clusterId is null
            ? _snapshots
            : _snapshots.Where(s => s.ClusterId == clusterId).ToList();

        return filtered.Sum(s => s.Amount);
    }

    public IReadOnlyList<RevenueSnapshot> GetSnapshots(DateTimeOffset? since = null)
    {
        return since is null
            ? _snapshots
            : _snapshots.Where(s => s.Timestamp >= since).ToList();
    }
}

public sealed record RevenueSnapshot(
    string SnapshotId,
    string ClusterId,
    decimal Amount,
    string Currency,
    DateTimeOffset Timestamp
);
