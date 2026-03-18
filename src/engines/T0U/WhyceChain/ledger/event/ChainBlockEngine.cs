namespace Whycespace.Engines.T0U.WhyceChain.Ledger.Event;

using global::System.Security.Cryptography;
using global::System.Text;
using Whycespace.Systems.Upstream.WhyceChain.Models;
using Whycespace.Systems.Upstream.WhyceChain.Stores;

public sealed class ChainBlockEngine
{
    private readonly ChainBlockStore _store;

    public ChainBlockEngine(ChainBlockStore store)
    {
        _store = store;
    }

    public ChainBlock CreateBlock(IReadOnlyList<string> entryIds, string merkleRoot)
    {
        var latest = _store.GetLatestBlock();
        var blockNumber = latest is null ? 0 : latest.BlockNumber + 1;
        var previousBlockHash = latest?.BlockHash ?? "genesis";

        var blockId = Guid.NewGuid().ToString();
        var timestamp = DateTimeOffset.UtcNow;
        var blockHash = ComputeBlockHash(blockNumber, previousBlockHash, merkleRoot, timestamp);

        var block = new ChainBlock(
            blockId,
            blockNumber,
            previousBlockHash,
            blockHash,
            merkleRoot,
            timestamp,
            entryIds);

        _store.AddBlock(block);
        return block;
    }

    public bool ValidateBlock(ChainBlock block)
    {
        var latest = _store.GetLatestBlock();

        if (latest is null)
            return block.BlockNumber == 0 && block.PreviousBlockHash == "genesis";

        return block.BlockNumber == latest.BlockNumber + 1
            && block.PreviousBlockHash == latest.BlockHash;
    }

    public ChainBlock GetBlock(long blockNumber)
    {
        return _store.GetBlock(blockNumber);
    }

    private static string ComputeBlockHash(
        long blockNumber,
        string previousBlockHash,
        string merkleRoot,
        DateTimeOffset timestamp)
    {
        var input = $"{blockNumber}:{previousBlockHash}:{merkleRoot}:{timestamp:O}";
        return Convert.ToBase64String(
            SHA256.HashData(Encoding.UTF8.GetBytes(input)));
    }
}
