namespace Whycespace.Engines.T0U.WhyceChain;

using Whycespace.Systems.Upstream.WhyceChain.Models;
using Whycespace.Systems.Upstream.WhyceChain.Stores;

public sealed class ChainIndexEngine
{
    private readonly ChainIndexStore _indexStore;
    private readonly ChainBlockStore _blockStore;
    private readonly ChainLedgerStore _ledgerStore;

    public ChainIndexEngine(
        ChainIndexStore indexStore,
        ChainBlockStore blockStore,
        ChainLedgerStore ledgerStore)
    {
        _indexStore = indexStore;
        _blockStore = blockStore;
        _ledgerStore = ledgerStore;
    }

    public void IndexBlock(long blockNumber)
    {
        var block = _blockStore.GetBlock(blockNumber);

        foreach (var entryId in block.EntryIds)
        {
            var entry = _ledgerStore.GetEntry(entryId);
            _indexStore.IndexEntry(entryId, entry.EventType, blockNumber, entry.Timestamp);
        }
    }

    public IReadOnlyList<ChainLedgerEntry> SearchEvents(string eventType)
    {
        var entryIds = _indexStore.GetByEventType(eventType);
        return entryIds.Select(id => _ledgerStore.GetEntry(id)).ToList();
    }

    public IReadOnlyList<ChainBlock> SearchBlocks(DateTimeOffset from, DateTimeOffset to)
    {
        var entryIds = _indexStore.GetByTimestampRange(from, to);

        return entryIds
            .Select(id => _ledgerStore.GetEntry(id))
            .Where(e => e.BlockId is not null)
            .Select(e => e.BlockId!)
            .Distinct()
            .Select(blockId => _blockStore.GetLatestBlock())
            .Where(b => b is not null)
            .Cast<ChainBlock>()
            .ToList();
    }
}
