namespace Whycespace.Engines.T0U.WhyceChain;

using global::System.Collections.Concurrent;
using Whycespace.Systems.Upstream.WhyceChain.Models;
using Whycespace.Systems.Upstream.WhyceChain.Stores;

public sealed class ChainSnapshotEngine
{
    private readonly ChainBlockStore _blockStore;
    private readonly ChainLedgerStore _ledgerStore;
    private readonly ConcurrentDictionary<string, ChainSnapshot> _snapshots = new();

    public ChainSnapshotEngine(ChainBlockStore blockStore, ChainLedgerStore ledgerStore)
    {
        _blockStore = blockStore;
        _ledgerStore = ledgerStore;
    }

    public ChainSnapshot CreateSnapshot()
    {
        var latest = _blockStore.GetLatestBlock();
        var totalEntries = _ledgerStore.GetAllEntries().Count;

        var snapshot = new ChainSnapshot(
            Guid.NewGuid().ToString(),
            latest?.BlockNumber ?? -1,
            latest?.BlockHash ?? "empty",
            totalEntries,
            DateTimeOffset.UtcNow);

        _snapshots[snapshot.SnapshotId] = snapshot;
        return snapshot;
    }

    public ChainSnapshot RestoreSnapshot(string snapshotId)
    {
        if (!_snapshots.TryGetValue(snapshotId, out var snapshot))
            throw new KeyNotFoundException($"Snapshot not found: {snapshotId}");

        return snapshot;
    }
}
