using Whycespace.Engines.T0U.WhyceChain;
using Whycespace.Systems.Upstream.WhyceChain.Ledger;

namespace Whycespace.WhyceChain.Tests;

public class ChainAppendEngineTests
{
    private readonly ChainAppendEngine _engine = new();

    private static ChainLedgerEntry CreateEntry(string payloadHash, DateTimeOffset timestamp, long sequenceNumber = 0)
    {
        return new ChainLedgerEntry(
            EntryId: Guid.NewGuid(),
            EntryType: "TestEvent",
            AggregateId: "test-aggregate",
            SequenceNumber: sequenceNumber,
            PayloadHash: payloadHash,
            MetadataHash: "meta-hash",
            PreviousEntryHash: "",
            EntryHash: ChainHashUtility.ComputeEntryHash(Guid.NewGuid().ToString(), payloadHash, ""),
            Timestamp: timestamp,
            TraceId: "trace-001",
            CorrelationId: "corr-001",
            EventVersion: 1);
    }

    private static (ChainBlock block, ChainAppendCommand command) CreateValidAppendScenario(
        long currentChainHeight = -1,
        string latestBlockHash = "",
        long blockHeight = 0,
        string? previousBlockHash = null)
    {
        var now = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var baseTime = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var entries = new List<ChainLedgerEntry>
        {
            CreateEntry("hash1", baseTime, 0),
            CreateEntry("hash2", baseTime.AddSeconds(1), 1)
        };

        var entryHashes = entries.Select(e => e.PayloadHash).ToList();
        var merkleRoot = ChainHashUtility.ComputeMerkleRoot(entryHashes);
        var blockHash = ChainHashUtility.ComputeBlockHash(blockHeight, previousBlockHash, merkleRoot, entries.Count, now);

        var block = new ChainBlock(
            Guid.NewGuid(),
            blockHeight,
            previousBlockHash,
            entries,
            merkleRoot,
            blockHash,
            entries.Count,
            now,
            "validator-sig",
            "trace-001");

        var command = new ChainAppendCommand(
            currentChainHeight,
            latestBlockHash,
            block,
            "trace-001",
            "corr-001",
            DateTime.UtcNow);

        return (block, command);
    }

    [Fact]
    public void Execute_ValidGenesisBlock_ShouldAccept()
    {
        var (_, command) = CreateValidAppendScenario();

        var result = _engine.Execute(command);

        Assert.True(result.BlockAccepted);
        Assert.Equal(0, result.NewChainHeight);
        Assert.True(result.ChainContinuityValid);
        Assert.Empty(result.ValidationErrors);
    }

    [Fact]
    public void Execute_ValidSequentialBlock_ShouldAccept()
    {
        var (genesis, _) = CreateValidAppendScenario();
        var (_, command) = CreateValidAppendScenario(
            currentChainHeight: 0,
            latestBlockHash: genesis.BlockHash,
            blockHeight: 1,
            previousBlockHash: genesis.BlockHash);

        var result = _engine.Execute(command);

        Assert.True(result.BlockAccepted);
        Assert.Equal(1, result.NewChainHeight);
        Assert.True(result.ChainContinuityValid);
    }

    [Fact]
    public void Execute_InvalidBlockHeight_ShouldReject()
    {
        var (_, command) = CreateValidAppendScenario(
            currentChainHeight: 0,
            latestBlockHash: "some-hash",
            blockHeight: 5,
            previousBlockHash: "some-hash");

        var result = _engine.Execute(command);

        Assert.False(result.BlockAccepted);
        Assert.False(result.ChainContinuityValid);
        Assert.Contains(result.ValidationErrors, e => e.Contains("BlockHeight mismatch"));
    }

    [Fact]
    public void Execute_InvalidPreviousHash_ShouldReject()
    {
        var (_, command) = CreateValidAppendScenario(
            currentChainHeight: 0,
            latestBlockHash: "correct-hash",
            blockHeight: 1,
            previousBlockHash: "wrong-hash");

        var result = _engine.Execute(command);

        Assert.False(result.BlockAccepted);
        Assert.False(result.ChainContinuityValid);
        Assert.Contains(result.ValidationErrors, e => e.Contains("PreviousBlockHash mismatch"));
    }

    [Fact]
    public void Execute_IsDeterministic()
    {
        var (_, command) = CreateValidAppendScenario();

        var result1 = _engine.Execute(command);
        var result2 = _engine.Execute(command);

        Assert.Equal(result1.BlockAccepted, result2.BlockAccepted);
        Assert.Equal(result1.NewChainHeight, result2.NewChainHeight);
        Assert.Equal(result1.AppendedBlockHash, result2.AppendedBlockHash);
        Assert.Equal(result1.ChainContinuityValid, result2.ChainContinuityValid);
    }
}
