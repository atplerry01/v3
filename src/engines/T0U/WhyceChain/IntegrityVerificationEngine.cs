namespace Whycespace.Engines.T0U.WhyceChain;

using Whycespace.System.Upstream.WhyceChain.Stores;

public sealed class IntegrityVerificationEngine
{
    private readonly ChainBlockStore _blockStore;
    private readonly ChainLedgerStore _ledgerStore;
    private readonly MerkleProofEngine _merkleEngine;

    public IntegrityVerificationEngine(
        ChainBlockStore blockStore,
        ChainLedgerStore ledgerStore,
        MerkleProofEngine merkleEngine)
    {
        _blockStore = blockStore;
        _ledgerStore = ledgerStore;
        _merkleEngine = merkleEngine;
    }

    public bool VerifyBlock(long blockNumber)
    {
        try
        {
            var block = _blockStore.GetBlock(blockNumber);
            var recomputedRoot = _merkleEngine.BuildTree(block.EntryIds);
            return recomputedRoot == block.MerkleRoot;
        }
        catch (KeyNotFoundException)
        {
            return false;
        }
    }

    public bool VerifyChain()
    {
        var latest = _blockStore.GetLatestBlock();
        if (latest is null)
            return true;

        for (long i = 0; i <= latest.BlockNumber; i++)
        {
            var block = _blockStore.GetBlock(i);

            if (i == 0 && block.PreviousBlockHash != "genesis")
                return false;

            if (i > 0)
            {
                var previous = _blockStore.GetBlock(i - 1);
                if (block.PreviousBlockHash != previous.BlockHash)
                    return false;
            }

            if (!VerifyBlock(i))
                return false;
        }

        return true;
    }

    public bool VerifyEntry(string entryId)
    {
        try
        {
            _ledgerStore.GetEntry(entryId);
            return true;
        }
        catch (KeyNotFoundException)
        {
            return false;
        }
    }
}
