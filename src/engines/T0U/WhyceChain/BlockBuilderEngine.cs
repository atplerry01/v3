namespace Whycespace.Engines.T0U.WhyceChain;

using Whycespace.System.Upstream.WhyceChain.Models;
using Whycespace.System.Upstream.WhyceChain.Stores;

public sealed class BlockBuilderEngine
{
    private readonly ChainLedgerStore _ledgerStore;
    private readonly ChainBlockEngine _blockEngine;
    private readonly MerkleProofEngine _merkleEngine;

    public BlockBuilderEngine(
        ChainLedgerStore ledgerStore,
        ChainBlockEngine blockEngine,
        MerkleProofEngine merkleEngine)
    {
        _ledgerStore = ledgerStore;
        _blockEngine = blockEngine;
        _merkleEngine = merkleEngine;
    }

    public IReadOnlyList<ChainLedgerEntry> CollectPendingEntries()
    {
        return _ledgerStore.GetAllEntries()
            .Where(e => e.BlockId is null)
            .OrderBy(e => e.Timestamp)
            .ToList();
    }

    public ChainBlock? BuildBlock()
    {
        var pending = CollectPendingEntries();
        if (pending.Count == 0)
            return null;

        var entryIds = pending.Select(e => e.EntryId).ToList();
        var merkleRoot = _merkleEngine.BuildTree(entryIds);
        var block = _blockEngine.CreateBlock(entryIds, merkleRoot);

        foreach (var entry in pending)
        {
            _ledgerStore.UpdateBlockId(entry.EntryId, block.BlockId);
        }

        return block;
    }
}
