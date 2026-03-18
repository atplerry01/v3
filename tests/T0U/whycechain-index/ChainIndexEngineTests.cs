using Whycespace.Engines.T3I.Reporting.Chain;
using Whycespace.Systems.Upstream.WhyceChain.Models;

namespace Whycespace.WhyceChainIndex.Tests;

public class ChainIndexEngineTests
{
    private readonly ChainIndexEngine _engine = new();

    [Fact]
    public void Execute_ShouldBuildEntryIndexBySequence()
    {
        var entries = new List<ChainLedgerEntry>
        {
            new("e-1", DateTimeOffset.UtcNow, "PolicyDecision", "hash-1", "prev-0", "b-1"),
            new("e-2", DateTimeOffset.UtcNow, "Transaction", "hash-2", "hash-1", "b-1")
        };
        var command = new ChainIndexCommand(entries, [], "trace-1", "corr-1", DateTime.UtcNow);

        var result = _engine.Execute(command);

        Assert.Equal("hash-1", result.EntryIndexBySequence[0]);
        Assert.Equal("hash-2", result.EntryIndexBySequence[1]);
        Assert.Equal(2, result.EntryIndexBySequence.Count);
    }

    [Fact]
    public void Execute_ShouldBuildEntryIndexByHash()
    {
        var entries = new List<ChainLedgerEntry>
        {
            new("e-1", DateTimeOffset.UtcNow, "PolicyDecision", "hash-1", "prev-0", "b-1"),
            new("e-2", DateTimeOffset.UtcNow, "Transaction", "hash-2", "hash-1", "b-1")
        };
        var command = new ChainIndexCommand(entries, [], "trace-1", "corr-1", DateTime.UtcNow);

        var result = _engine.Execute(command);

        Assert.Equal(0L, result.EntryIndexByHash["hash-1"]);
        Assert.Equal(1L, result.EntryIndexByHash["hash-2"]);
    }

    [Fact]
    public void Execute_ShouldBuildBlockIndexByHeight()
    {
        var blocks = new List<ChainBlock>
        {
            new("b-1", 0, "", "blockhash-0", "merkle-0", DateTimeOffset.UtcNow, ["e-1"]),
            new("b-2", 1, "blockhash-0", "blockhash-1", "merkle-1", DateTimeOffset.UtcNow, ["e-2"])
        };
        var command = new ChainIndexCommand([], blocks, "trace-1", "corr-1", DateTime.UtcNow);

        var result = _engine.Execute(command);

        Assert.Equal("blockhash-0", result.BlockIndexByHeight[0]);
        Assert.Equal("blockhash-1", result.BlockIndexByHeight[1]);
    }

    [Fact]
    public void Execute_ShouldBuildBlockIndexByHash()
    {
        var blocks = new List<ChainBlock>
        {
            new("b-1", 0, "", "blockhash-0", "merkle-0", DateTimeOffset.UtcNow, ["e-1"]),
            new("b-2", 1, "blockhash-0", "blockhash-1", "merkle-1", DateTimeOffset.UtcNow, ["e-2"])
        };
        var command = new ChainIndexCommand([], blocks, "trace-1", "corr-1", DateTime.UtcNow);

        var result = _engine.Execute(command);

        Assert.Equal(0L, result.BlockIndexByHash["blockhash-0"]);
        Assert.Equal(1L, result.BlockIndexByHash["blockhash-1"]);
    }

    [Fact]
    public void Execute_ShouldBuildTraceIndex()
    {
        var entries = new List<ChainLedgerEntry>
        {
            new("e-1", DateTimeOffset.UtcNow, "PolicyDecision", "hash-1", "prev-0", "b-1"),
            new("e-2", DateTimeOffset.UtcNow, "Transaction", "hash-2", "hash-1", "b-1")
        };
        var command = new ChainIndexCommand(entries, [], "trace-42", "corr-1", DateTime.UtcNow);

        var result = _engine.Execute(command);

        Assert.True(result.TraceIndex.ContainsKey("trace-42"));
        Assert.Equal(new List<string> { "hash-1", "hash-2" }, result.TraceIndex["trace-42"]);
    }

    [Fact]
    public void Execute_ShouldBuildCorrelationIndex()
    {
        var entries = new List<ChainLedgerEntry>
        {
            new("e-1", DateTimeOffset.UtcNow, "PolicyDecision", "hash-1", "prev-0", "b-1"),
            new("e-2", DateTimeOffset.UtcNow, "Transaction", "hash-2", "hash-1", "b-1")
        };
        var command = new ChainIndexCommand(entries, [], "trace-1", "corr-99", DateTime.UtcNow);

        var result = _engine.Execute(command);

        Assert.True(result.CorrelationIndex.ContainsKey("corr-99"));
        Assert.Equal(new List<string> { "hash-1", "hash-2" }, result.CorrelationIndex["corr-99"]);
    }

    [Fact]
    public void Execute_ShouldBeDeterministic()
    {
        var timestamp = DateTime.UtcNow;
        var entries = new List<ChainLedgerEntry>
        {
            new("e-1", DateTimeOffset.UtcNow, "PolicyDecision", "hash-1", "prev-0", "b-1"),
            new("e-2", DateTimeOffset.UtcNow, "Transaction", "hash-2", "hash-1", "b-1")
        };
        var blocks = new List<ChainBlock>
        {
            new("b-1", 0, "", "blockhash-0", "merkle-0", DateTimeOffset.UtcNow, ["e-1", "e-2"])
        };
        var command = new ChainIndexCommand(entries, blocks, "trace-1", "corr-1", timestamp);

        var result1 = _engine.Execute(command);
        var result2 = _engine.Execute(command);

        Assert.Equal(result1.EntryIndexBySequence, result2.EntryIndexBySequence);
        Assert.Equal(result1.EntryIndexByHash, result2.EntryIndexByHash);
        Assert.Equal(result1.BlockIndexByHeight, result2.BlockIndexByHeight);
        Assert.Equal(result1.BlockIndexByHash, result2.BlockIndexByHash);
        Assert.Equal(result1.GeneratedAt, result2.GeneratedAt);
        Assert.Equal(result1.TraceId, result2.TraceId);
    }

    [Fact]
    public void Execute_EmptyInput_ShouldReturnEmptyIndexes()
    {
        var command = new ChainIndexCommand([], [], "trace-1", "corr-1", DateTime.UtcNow);

        var result = _engine.Execute(command);

        Assert.Empty(result.EntryIndexBySequence);
        Assert.Empty(result.EntryIndexByHash);
        Assert.Empty(result.BlockIndexByHeight);
        Assert.Empty(result.BlockIndexByHash);
        Assert.Empty(result.TraceIndex);
        Assert.Empty(result.CorrelationIndex);
    }
}
