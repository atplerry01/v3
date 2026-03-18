using Whycespace.Engines.T3I.Reporting.Chain;
using Whycespace.Systems.Upstream.WhyceChain.Models;
using ChainHashUtility = Whycespace.Systems.Upstream.WhyceChain.Ledger.ChainHashUtility;

namespace Whycespace.WhyceChainAudit.Tests;

public class ChainAuditEngineTests
{
    private readonly ChainAuditEngine _engine = new();

    private static ChainBlock CreateBlock(
        string blockId,
        long blockNumber,
        string previousBlockHash,
        IReadOnlyList<string> entryIds,
        IReadOnlyList<string> payloadHashes,
        DateTimeOffset timestamp)
    {
        var merkleRoot = ChainHashUtility.ComputeMerkleRoot(payloadHashes.ToList());
        var blockHash = ChainHashUtility.GenerateBlockHash(
            Guid.Parse(blockId),
            blockNumber,
            merkleRoot,
            previousBlockHash,
            entryIds.Count,
            timestamp);

        return new ChainBlock(blockId, blockNumber, previousBlockHash, blockHash, merkleRoot, timestamp, entryIds);
    }

    [Fact]
    public void Execute_ValidChain_ShouldReportNoAnomalies()
    {
        var timestamp = DateTimeOffset.UtcNow;
        var entries = new List<ChainLedgerEntry>
        {
            new("e-1", timestamp, "PolicyDecision", "hash-1", "genesis", "b-1"),
            new("e-2", timestamp.AddSeconds(1), "Transaction", "hash-2", "hash-1", "b-1")
        };

        var blockId = Guid.NewGuid().ToString("D");
        var block = CreateBlock(blockId, 0, "genesis", ["e-1", "e-2"], ["hash-1", "hash-2"], timestamp);

        var command = new ChainAuditCommand(entries, [block], null, "trace-1", "corr-1", DateTime.UtcNow);
        var result = _engine.Execute(command);

        Assert.Equal(1, result.TotalBlocks);
        Assert.Equal(2, result.TotalEntries);
        Assert.Equal(0, result.BrokenBlockLinks);
        Assert.Equal(0, result.InvalidBlockHashes);
        Assert.Equal(0, result.MerkleRootMismatches);
        Assert.Equal(0, result.SequenceGaps);
        Assert.False(result.AnomalyDetected);
    }

    [Fact]
    public void Execute_BrokenBlockLink_ShouldDetect()
    {
        var timestamp = DateTimeOffset.UtcNow;
        var entries = new List<ChainLedgerEntry>
        {
            new("e-1", timestamp, "PolicyDecision", "hash-1", "genesis", "b-1"),
            new("e-2", timestamp.AddSeconds(1), "Transaction", "hash-2", "hash-1", "b-2")
        };

        var blockId1 = Guid.NewGuid().ToString("D");
        var block1 = CreateBlock(blockId1, 0, "genesis", ["e-1"], ["hash-1"], timestamp);

        var blockId2 = Guid.NewGuid().ToString("D");
        // Intentionally use wrong previous block hash
        var block2 = CreateBlock(blockId2, 1, "wrong-hash", ["e-2"], ["hash-2"], timestamp.AddSeconds(2));

        var command = new ChainAuditCommand(entries, [block1, block2], null, "trace-1", "corr-1", DateTime.UtcNow);
        var result = _engine.Execute(command);

        Assert.True(result.BrokenBlockLinks > 0);
        Assert.True(result.AnomalyDetected);
    }

    [Fact]
    public void Execute_InvalidBlockHash_ShouldDetect()
    {
        var timestamp = DateTimeOffset.UtcNow;
        var entries = new List<ChainLedgerEntry>
        {
            new("e-1", timestamp, "PolicyDecision", "hash-1", "genesis", "b-1")
        };

        var blockId = Guid.NewGuid().ToString("D");
        var merkleRoot = ChainHashUtility.ComputeMerkleRoot(["hash-1"]);
        // Create block with tampered hash
        var block = new ChainBlock(blockId, 0, "genesis", "tampered-hash", merkleRoot, timestamp, ["e-1"]);

        var command = new ChainAuditCommand(entries, [block], null, "trace-1", "corr-1", DateTime.UtcNow);
        var result = _engine.Execute(command);

        Assert.True(result.InvalidBlockHashes > 0);
        Assert.True(result.AnomalyDetected);
    }

    [Fact]
    public void Execute_MerkleRootMismatch_ShouldDetect()
    {
        var timestamp = DateTimeOffset.UtcNow;
        var entries = new List<ChainLedgerEntry>
        {
            new("e-1", timestamp, "PolicyDecision", "hash-1", "genesis", "b-1")
        };

        var blockId = Guid.NewGuid().ToString("D");
        var wrongMerkle = "wrong-merkle-root";
        var blockHash = ChainHashUtility.GenerateBlockHash(
            Guid.Parse(blockId), 0, wrongMerkle, "genesis", 1, timestamp);
        // Block has valid hash for wrong merkle, but merkle doesn't match entries
        var block = new ChainBlock(blockId, 0, "genesis", blockHash, wrongMerkle, timestamp, ["e-1"]);

        var command = new ChainAuditCommand(entries, [block], null, "trace-1", "corr-1", DateTime.UtcNow);
        var result = _engine.Execute(command);

        Assert.True(result.MerkleRootMismatches > 0);
        Assert.True(result.AnomalyDetected);
    }

    [Fact]
    public void Execute_SequenceGap_ShouldDetect()
    {
        var timestamp = DateTimeOffset.UtcNow;
        var entries = new List<ChainLedgerEntry>
        {
            new("e-1", timestamp, "PolicyDecision", "hash-1", "genesis", "b-1"),
            new("e-2", timestamp.AddSeconds(1), "Transaction", "hash-2", "hash-1", "b-2")
        };

        var blockId1 = Guid.NewGuid().ToString("D");
        var block1 = CreateBlock(blockId1, 0, "genesis", ["e-1"], ["hash-1"], timestamp);

        // Skip block 1, jump to block 2 (height gap)
        var blockId2 = Guid.NewGuid().ToString("D");
        var block2 = CreateBlock(blockId2, 3, block1.BlockHash, ["e-2"], ["hash-2"], timestamp.AddSeconds(2));

        var command = new ChainAuditCommand(entries, [block1, block2], null, "trace-1", "corr-1", DateTime.UtcNow);
        var result = _engine.Execute(command);

        Assert.True(result.SequenceGaps > 0);
        Assert.True(result.AnomalyDetected);
    }

    [Fact]
    public void Execute_EmptyChain_ShouldReportNoAnomalies()
    {
        var command = new ChainAuditCommand([], [], null, "trace-1", "corr-1", DateTime.UtcNow);
        var result = _engine.Execute(command);

        Assert.Equal(0, result.TotalBlocks);
        Assert.Equal(0, result.TotalEntries);
        Assert.False(result.AnomalyDetected);
    }

    [Fact]
    public void Execute_ShouldBeDeterministic()
    {
        var timestamp = DateTime.UtcNow;
        var entries = new List<ChainLedgerEntry>
        {
            new("e-1", DateTimeOffset.UtcNow, "PolicyDecision", "hash-1", "genesis", "b-1")
        };

        var blockId = Guid.NewGuid().ToString("D");
        var block = CreateBlock(blockId, 0, "genesis", ["e-1"], ["hash-1"], DateTimeOffset.UtcNow);

        var command = new ChainAuditCommand(entries, [block], null, "trace-1", "corr-1", timestamp);

        var result1 = _engine.Execute(command);
        var result2 = _engine.Execute(command);

        Assert.Equal(result1.TotalBlocks, result2.TotalBlocks);
        Assert.Equal(result1.TotalEntries, result2.TotalEntries);
        Assert.Equal(result1.BrokenBlockLinks, result2.BrokenBlockLinks);
        Assert.Equal(result1.InvalidBlockHashes, result2.InvalidBlockHashes);
        Assert.Equal(result1.MerkleRootMismatches, result2.MerkleRootMismatches);
        Assert.Equal(result1.SequenceGaps, result2.SequenceGaps);
        Assert.Equal(result1.AnomalyDetected, result2.AnomalyDetected);
        Assert.Equal(result1.AuditTimestamp, result2.AuditTimestamp);
        Assert.Equal(result1.TraceId, result2.TraceId);
    }

    [Fact]
    public void Execute_MultiBlockValidChain_ShouldPass()
    {
        var timestamp = DateTimeOffset.UtcNow;
        var entries = new List<ChainLedgerEntry>
        {
            new("e-1", timestamp, "PolicyDecision", "hash-1", "genesis", "b-1"),
            new("e-2", timestamp.AddSeconds(1), "Transaction", "hash-2", "hash-1", "b-2"),
            new("e-3", timestamp.AddSeconds(2), "Audit", "hash-3", "hash-2", "b-3")
        };

        var blockId1 = Guid.NewGuid().ToString("D");
        var block1 = CreateBlock(blockId1, 0, "genesis", ["e-1"], ["hash-1"], timestamp);

        var blockId2 = Guid.NewGuid().ToString("D");
        var block2 = CreateBlock(blockId2, 1, block1.BlockHash, ["e-2"], ["hash-2"], timestamp.AddSeconds(1));

        var blockId3 = Guid.NewGuid().ToString("D");
        var block3 = CreateBlock(blockId3, 2, block2.BlockHash, ["e-3"], ["hash-3"], timestamp.AddSeconds(2));

        var command = new ChainAuditCommand(entries, [block1, block2, block3], null, "trace-1", "corr-1", DateTime.UtcNow);
        var result = _engine.Execute(command);

        Assert.Equal(3, result.TotalBlocks);
        Assert.Equal(3, result.TotalEntries);
        Assert.Equal(0, result.BrokenBlockLinks);
        Assert.Equal(0, result.InvalidBlockHashes);
        Assert.Equal(0, result.MerkleRootMismatches);
        Assert.Equal(0, result.SequenceGaps);
        Assert.False(result.AnomalyDetected);
    }
}
