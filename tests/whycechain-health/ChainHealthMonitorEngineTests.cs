using Whycespace.Engines.T3I.Monitoring.Chain;
using Whycespace.Systems.Upstream.WhyceChain.Models;

namespace Whycespace.WhyceChainHealth.Tests;

public class ChainHealthMonitorEngineTests
{
    private readonly ChainHealthMonitorEngine _engine = new();

    private static List<ChainBlock> CreateValidBlocks(int count)
    {
        var blocks = new List<ChainBlock>();
        var previousHash = "";

        for (var i = 0; i < count; i++)
        {
            var hash = $"blockhash-{i}";
            blocks.Add(new ChainBlock($"b-{i}", i, previousHash, hash, $"merkle-{i}", DateTimeOffset.UtcNow, [$"e-{i}"]));
            previousHash = hash;
        }

        return blocks;
    }

    private static List<ChainLedgerEntry> CreateValidEntries(int count)
    {
        var entries = new List<ChainLedgerEntry>();
        var previousHash = "genesis";

        for (var i = 0; i < count; i++)
        {
            var hash = $"payload-hash-{i}";
            entries.Add(new ChainLedgerEntry($"e-{i}", DateTimeOffset.UtcNow, "Transaction", hash, previousHash, $"b-{i}"));
            previousHash = hash;
        }

        return entries;
    }

    [Fact]
    public void Execute_HealthyChain_ShouldReturnHealthyStatus()
    {
        var blocks = CreateValidBlocks(5);
        var entries = CreateValidEntries(5);
        var timestamp = DateTime.UtcNow;
        var command = new ChainHealthMonitorCommand(
            blocks, entries,
            LatestSnapshotHeight: 3,
            ReplicationHeight: 4,
            AnchoredBlockHeight: 4,
            TraceId: "trace-1",
            CorrelationId: "corr-1",
            Timestamp: timestamp);

        var result = _engine.Execute(command);

        Assert.Equal("Healthy", result.ChainHealthStatus);
        Assert.Equal("Valid", result.BlockContinuityStatus);
        Assert.Equal("Valid", result.LedgerIntegrityStatus);
        Assert.Equal(4L, result.CurrentChainHeight);
        Assert.Equal(3L, result.SnapshotHeight);
        Assert.Equal(0L, result.ReplicationLag);
        Assert.Equal(0L, result.AnchorLag);
        Assert.Equal("trace-1", result.TraceId);
        Assert.Equal(timestamp, result.HealthTimestamp);
    }

    [Fact]
    public void Execute_ReplicationLag_ShouldReturnDegradedStatus()
    {
        var blocks = CreateValidBlocks(10);
        var entries = CreateValidEntries(10);
        var command = new ChainHealthMonitorCommand(
            blocks, entries,
            LatestSnapshotHeight: 5,
            ReplicationHeight: 3,
            AnchoredBlockHeight: 9,
            TraceId: "trace-2",
            CorrelationId: "corr-2",
            Timestamp: DateTime.UtcNow);

        var result = _engine.Execute(command);

        Assert.Equal("Degraded", result.ChainHealthStatus);
        Assert.Equal(6L, result.ReplicationLag);
    }

    [Fact]
    public void Execute_AnchorLag_ShouldReturnDegradedStatus()
    {
        var blocks = CreateValidBlocks(20);
        var entries = CreateValidEntries(20);
        var command = new ChainHealthMonitorCommand(
            blocks, entries,
            LatestSnapshotHeight: 10,
            ReplicationHeight: 19,
            AnchoredBlockHeight: 5,
            TraceId: "trace-3",
            CorrelationId: "corr-3",
            Timestamp: DateTime.UtcNow);

        var result = _engine.Execute(command);

        Assert.Equal("Degraded", result.ChainHealthStatus);
        Assert.Equal(14L, result.AnchorLag);
    }

    [Fact]
    public void Execute_BlockContinuityFailure_ShouldReturnCriticalStatus()
    {
        var blocks = new List<ChainBlock>
        {
            new("b-0", 0, "", "blockhash-0", "merkle-0", DateTimeOffset.UtcNow, ["e-0"]),
            new("b-1", 1, "blockhash-0", "blockhash-1", "merkle-1", DateTimeOffset.UtcNow, ["e-1"]),
            new("b-2", 3, "blockhash-1", "blockhash-3", "merkle-3", DateTimeOffset.UtcNow, ["e-3"])
        };
        var entries = CreateValidEntries(3);
        var command = new ChainHealthMonitorCommand(
            blocks, entries,
            LatestSnapshotHeight: 0,
            ReplicationHeight: 3,
            AnchoredBlockHeight: 3,
            TraceId: "trace-4",
            CorrelationId: "corr-4",
            Timestamp: DateTime.UtcNow);

        var result = _engine.Execute(command);

        Assert.Equal("Critical", result.ChainHealthStatus);
        Assert.Equal("Broken", result.BlockContinuityStatus);
    }

    [Fact]
    public void Execute_BrokenBlockHashLinkage_ShouldReturnCritical()
    {
        var blocks = new List<ChainBlock>
        {
            new("b-0", 0, "", "blockhash-0", "merkle-0", DateTimeOffset.UtcNow, ["e-0"]),
            new("b-1", 1, "wrong-hash", "blockhash-1", "merkle-1", DateTimeOffset.UtcNow, ["e-1"]),
            new("b-2", 2, "blockhash-1", "blockhash-2", "merkle-2", DateTimeOffset.UtcNow, ["e-2"])
        };
        var entries = CreateValidEntries(3);
        var command = new ChainHealthMonitorCommand(
            blocks, entries,
            LatestSnapshotHeight: 0,
            ReplicationHeight: 2,
            AnchoredBlockHeight: 2,
            TraceId: "trace-5",
            CorrelationId: "corr-5",
            Timestamp: DateTime.UtcNow);

        var result = _engine.Execute(command);

        Assert.Equal("Critical", result.ChainHealthStatus);
        Assert.Equal("Broken", result.BlockContinuityStatus);
    }

    [Fact]
    public void Execute_LedgerSequenceGap_ShouldReturnCritical()
    {
        var entries = new List<ChainLedgerEntry>
        {
            new("e-0", DateTimeOffset.UtcNow, "Transaction", "hash-0", "genesis", "b-0"),
            new("e-1", DateTimeOffset.UtcNow, "Transaction", "hash-1", "hash-0", "b-0"),
            new("e-2", DateTimeOffset.UtcNow, "Transaction", "hash-2", "wrong-prev", "b-1")
        };
        var blocks = CreateValidBlocks(2);
        var command = new ChainHealthMonitorCommand(
            blocks, entries,
            LatestSnapshotHeight: 0,
            ReplicationHeight: 1,
            AnchoredBlockHeight: 1,
            TraceId: "trace-6",
            CorrelationId: "corr-6",
            Timestamp: DateTime.UtcNow);

        var result = _engine.Execute(command);

        Assert.Equal("Critical", result.ChainHealthStatus);
        Assert.Equal("Broken", result.LedgerIntegrityStatus);
    }

    [Fact]
    public void Execute_SnapshotExceedsChainHeight_ShouldReturnDegraded()
    {
        var blocks = CreateValidBlocks(3);
        var entries = CreateValidEntries(3);
        var command = new ChainHealthMonitorCommand(
            blocks, entries,
            LatestSnapshotHeight: 10,
            ReplicationHeight: 2,
            AnchoredBlockHeight: 2,
            TraceId: "trace-7",
            CorrelationId: "corr-7",
            Timestamp: DateTime.UtcNow);

        var result = _engine.Execute(command);

        Assert.Equal("Degraded", result.ChainHealthStatus);
    }

    [Fact]
    public void Execute_EmptyChain_ShouldReturnHealthy()
    {
        var command = new ChainHealthMonitorCommand(
            [], [],
            LatestSnapshotHeight: 0,
            ReplicationHeight: 0,
            AnchoredBlockHeight: 0,
            TraceId: "trace-8",
            CorrelationId: "corr-8",
            Timestamp: DateTime.UtcNow);

        var result = _engine.Execute(command);

        Assert.Equal("Healthy", result.ChainHealthStatus);
        Assert.Equal("Valid", result.BlockContinuityStatus);
        Assert.Equal("Valid", result.LedgerIntegrityStatus);
        Assert.Equal(0L, result.CurrentChainHeight);
        Assert.Equal(0L, result.ReplicationLag);
        Assert.Equal(0L, result.AnchorLag);
    }

    [Fact]
    public void Execute_ShouldBeDeterministic()
    {
        var timestamp = DateTime.UtcNow;
        var blocks = CreateValidBlocks(5);
        var entries = CreateValidEntries(5);
        var command = new ChainHealthMonitorCommand(
            blocks, entries,
            LatestSnapshotHeight: 3,
            ReplicationHeight: 4,
            AnchoredBlockHeight: 4,
            TraceId: "trace-det",
            CorrelationId: "corr-det",
            Timestamp: timestamp);

        var result1 = _engine.Execute(command);
        var result2 = _engine.Execute(command);

        Assert.Equal(result1.CurrentChainHeight, result2.CurrentChainHeight);
        Assert.Equal(result1.ReplicationLag, result2.ReplicationLag);
        Assert.Equal(result1.AnchorLag, result2.AnchorLag);
        Assert.Equal(result1.ChainHealthStatus, result2.ChainHealthStatus);
        Assert.Equal(result1.BlockContinuityStatus, result2.BlockContinuityStatus);
        Assert.Equal(result1.LedgerIntegrityStatus, result2.LedgerIntegrityStatus);
        Assert.Equal(result1.HealthTimestamp, result2.HealthTimestamp);
        Assert.Equal(result1.TraceId, result2.TraceId);
    }
}
