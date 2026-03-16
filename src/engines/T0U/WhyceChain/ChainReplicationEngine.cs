namespace Whycespace.Engines.T0U.WhyceChain;

using global::System.Collections.Concurrent;
using Whycespace.Systems.Upstream.WhyceChain.Models;
using Whycespace.Systems.Upstream.WhyceChain.Stores;

public sealed class ChainReplicationEngine
{
    private readonly ChainBlockStore _blockStore;
    private readonly IntegrityVerificationEngine _integrityEngine;
    private readonly ConcurrentDictionary<string, ChainNodeState> _nodes = new();

    public ChainReplicationEngine(
        ChainBlockStore blockStore,
        IntegrityVerificationEngine integrityEngine)
    {
        _blockStore = blockStore;
        _integrityEngine = integrityEngine;
    }

    public ChainBlock ReplicateBlock(string nodeId, long blockNumber)
    {
        var block = _blockStore.GetBlock(blockNumber);

        _nodes[nodeId] = new ChainNodeState(
            nodeId,
            block.BlockNumber,
            block.BlockHash,
            DateTimeOffset.UtcNow);

        return block;
    }

    public IReadOnlyList<ChainBlock> SyncNode(string nodeId)
    {
        var startBlock = 0L;

        if (_nodes.TryGetValue(nodeId, out var state))
            startBlock = state.LatestBlockNumber + 1;

        var latest = _blockStore.GetLatestBlock();
        if (latest is null)
            return [];

        var blocks = new List<ChainBlock>();
        for (var i = startBlock; i <= latest.BlockNumber; i++)
        {
            blocks.Add(ReplicateBlock(nodeId, i));
        }

        return blocks;
    }

    public bool VerifyNode(string nodeId)
    {
        if (!_nodes.TryGetValue(nodeId, out var state))
            return false;

        var latest = _blockStore.GetLatestBlock();
        if (latest is null)
            return state.LatestBlockNumber == -1;

        if (state.LatestBlockNumber != latest.BlockNumber)
            return false;

        if (state.LatestBlockHash != latest.BlockHash)
            return false;

        var blocks = new List<ChainBlock>();
        for (long i = 0; i <= latest.BlockNumber; i++)
        {
            try { blocks.Add(_blockStore.GetBlock(i)); }
            catch (KeyNotFoundException) { return false; }
        }

        var command = new IntegrityVerificationCommand(
            Array.Empty<ChainLedgerEntry>(),
            blocks,
            MerkleProof: null,
            TraceId: $"replication-verify-{nodeId}",
            CorrelationId: $"replication-{nodeId}",
            Timestamp: DateTimeOffset.UtcNow);
        var result = _integrityEngine.Execute(command);
        return result.BlockChainValid && result.MerkleRootValid;
    }
}
