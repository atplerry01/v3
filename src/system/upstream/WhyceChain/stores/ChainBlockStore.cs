namespace Whycespace.System.Upstream.WhyceChain.Stores;

using global::System.Collections.Concurrent;
using Whycespace.System.Upstream.WhyceChain.Models;

public sealed class ChainBlockStore
{
    private readonly ConcurrentDictionary<long, ChainBlock> _blocks = new();

    public void AddBlock(ChainBlock block)
    {
        if (!_blocks.TryAdd(block.BlockNumber, block))
            throw new InvalidOperationException($"Block number already exists: {block.BlockNumber}");
    }

    public ChainBlock GetBlock(long blockNumber)
    {
        if (!_blocks.TryGetValue(blockNumber, out var block))
            throw new KeyNotFoundException($"Block not found: {blockNumber}");

        return block;
    }

    public ChainBlock? GetLatestBlock()
    {
        if (_blocks.IsEmpty)
            return null;

        var maxNumber = _blocks.Keys.Max();
        return _blocks[maxNumber];
    }

    public ChainBlock GetBlockByNumber(long blockNumber)
    {
        return GetBlock(blockNumber);
    }
}
