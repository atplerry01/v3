using Whycespace.Engines.T3I.Reporting.Chain;
using Whycespace.Systems.Upstream.WhyceChain.Models;

namespace Whycespace.WhyceChainReplication.Tests;

public class ChainReplicationEngineTests
{
    private readonly ChainReplicationEngine _engine = new();

    private static List<ChainBlock> CreateBlocks(int count)
    {
        var blocks = new List<ChainBlock>();
        for (var i = 0; i < count; i++)
        {
            var entryIds = new List<string> { $"e-{i}-1", $"e-{i}-2" };
            blocks.Add(new ChainBlock(
                $"b-{i}", i, i == 0 ? "" : $"blockhash-{i - 1}",
                $"blockhash-{i}", $"merkle-{i}", DateTimeOffset.UtcNow, entryIds));
        }
        return blocks;
    }

    private static List<ChainLedgerEntry> CreateEntries(List<ChainBlock> blocks)
    {
        var entries = new List<ChainLedgerEntry>();
        foreach (var block in blocks)
        {
            foreach (var entryId in block.EntryIds)
            {
                entries.Add(new ChainLedgerEntry(
                    entryId, DateTimeOffset.UtcNow, "GovernanceAction",
                    $"payload-{entryId}", "", block.BlockId));
            }
        }
        return entries;
    }

    [Fact]
    public void Execute_ShouldSelectBlocksUpToTargetHeight()
    {
        var blocks = CreateBlocks(5);
        var entries = CreateEntries(blocks);
        var command = new ChainReplicationCommand(
            blocks, entries, TargetHeight: 2, "trace-1", "corr-1", DateTime.UtcNow);

        var result = _engine.Execute(command);

        Assert.Equal(3, result.ReplicationBlocks.Count);
        Assert.Equal(0, result.ReplicationBlocks[0].BlockNumber);
        Assert.Equal(1, result.ReplicationBlocks[1].BlockNumber);
        Assert.Equal(2, result.ReplicationBlocks[2].BlockNumber);
        Assert.Equal(2, result.ReplicatedHeight);
    }

    [Fact]
    public void Execute_ShouldSelectLedgerEntriesForReplicatedBlocks()
    {
        var blocks = CreateBlocks(5);
        var entries = CreateEntries(blocks);
        var command = new ChainReplicationCommand(
            blocks, entries, TargetHeight: 1, "trace-1", "corr-1", DateTime.UtcNow);

        var result = _engine.Execute(command);

        Assert.Equal(4, result.ReplicationEntries.Count);
        Assert.Equal(4, result.ReplicationEntryCount);
        Assert.All(result.ReplicationEntries, e =>
        {
            var blockId = e.BlockId;
            Assert.True(blockId == "b-0" || blockId == "b-1");
        });
    }

    [Fact]
    public void Execute_ShouldGenerateDeterministicReplicationHash()
    {
        var timestamp = DateTime.UtcNow;
        var blocks = CreateBlocks(3);
        var entries = CreateEntries(blocks);
        var command = new ChainReplicationCommand(
            blocks, entries, TargetHeight: 2, "trace-1", "corr-1", timestamp);

        var result1 = _engine.Execute(command);
        var result2 = _engine.Execute(command);

        Assert.Equal(result1.ReplicationHash, result2.ReplicationHash);
        Assert.NotEmpty(result1.ReplicationHash);
    }

    [Fact]
    public void Execute_ShouldThrowForInvalidTargetHeight()
    {
        var blocks = CreateBlocks(3);
        var entries = CreateEntries(blocks);
        var command = new ChainReplicationCommand(
            blocks, entries, TargetHeight: 10, "trace-1", "corr-1", DateTime.UtcNow);

        Assert.Throws<ArgumentException>(() => _engine.Execute(command));
    }

    [Fact]
    public void Execute_ShouldThrowForNegativeTargetHeight()
    {
        var blocks = CreateBlocks(3);
        var entries = CreateEntries(blocks);
        var command = new ChainReplicationCommand(
            blocks, entries, TargetHeight: -1, "trace-1", "corr-1", DateTime.UtcNow);

        Assert.Throws<ArgumentException>(() => _engine.Execute(command));
    }

    [Fact]
    public void Execute_ShouldPreserveTraceId()
    {
        var blocks = CreateBlocks(2);
        var entries = CreateEntries(blocks);
        var command = new ChainReplicationCommand(
            blocks, entries, TargetHeight: 1, "trace-42", "corr-1", DateTime.UtcNow);

        var result = _engine.Execute(command);

        Assert.Equal("trace-42", result.TraceId);
    }

    [Fact]
    public void Execute_ShouldUseCommandTimestampAsGeneratedAt()
    {
        var timestamp = new DateTime(2026, 3, 15, 12, 0, 0, DateTimeKind.Utc);
        var blocks = CreateBlocks(2);
        var entries = CreateEntries(blocks);
        var command = new ChainReplicationCommand(
            blocks, entries, TargetHeight: 1, "trace-1", "corr-1", timestamp);

        var result = _engine.Execute(command);

        Assert.Equal(timestamp, result.GeneratedAt);
    }

    [Fact]
    public void Execute_DifferentHeights_ShouldProduceDifferentHashes()
    {
        var blocks = CreateBlocks(5);
        var entries = CreateEntries(blocks);

        var result1 = _engine.Execute(new ChainReplicationCommand(
            blocks, entries, TargetHeight: 2, "trace-1", "corr-1", DateTime.UtcNow));
        var result2 = _engine.Execute(new ChainReplicationCommand(
            blocks, entries, TargetHeight: 4, "trace-1", "corr-1", DateTime.UtcNow));

        Assert.NotEqual(result1.ReplicationHash, result2.ReplicationHash);
    }
}
