using System.Security.Cryptography;
using System.Text;
using Whycespace.Engines.T3I.Reporting.Chain.Engines;
using Whycespace.Engines.T3I.Reporting.Chain.Models;
using Whycespace.Engines.T3I.Shared;
using Whycespace.Systems.Upstream.WhyceChain.Ledger;

namespace Whycespace.WhyceChain.Recovery.Tests;

public class ChainRecoveryEngineTests
{
    private readonly ChainRecoveryEngine _engine = new();

    private static ChainLedgerEntry CreateEntry(Guid entryId, long sequence, string previousHash)
    {
        var payloadHash = ComputeHash($"payload-{entryId}");
        var metadataHash = ComputeHash($"metadata-{entryId}");
        var entryHash = ChainHashUtility.ComputeEntryHash(entryId.ToString(), payloadHash, previousHash);

        return new ChainLedgerEntry(
            entryId,
            "GovernanceAction",
            "aggregate-1",
            sequence,
            payloadHash,
            metadataHash,
            previousHash,
            entryHash,
            DateTimeOffset.UtcNow,
            "trace-1",
            "corr-1",
            1);
    }

    private static ChainBlock CreateBlock(
        long height,
        string? previousBlockHash,
        IReadOnlyList<ChainLedgerEntry> entries,
        DateTime createdAt)
    {
        var entryHashes = entries.Select(e => e.EntryHash).ToList();
        var merkleRoot = ChainHashUtility.ComputeMerkleRoot(entryHashes);
        var blockHash = ChainHashUtility.ComputeBlockHash(height, previousBlockHash, merkleRoot, entries.Count, createdAt);

        return new ChainBlock(
            Guid.NewGuid(),
            height,
            previousBlockHash,
            entries,
            merkleRoot,
            blockHash,
            entries.Count,
            createdAt,
            "validator-sig",
            "trace-1");
    }

    private static string ComputeHash(string input)
    {
        return Convert.ToHexString(
            SHA256.HashData(Encoding.UTF8.GetBytes(input))).ToLowerInvariant();
    }

    [Fact]
    public void Execute_ValidRecovery_ReturnsRecoveredChain()
    {
        var snapshotHash = ComputeHash("genesis");
        var createdAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        var entry1 = CreateEntry(Guid.NewGuid(), 0, ComputeHash("prev-0"));
        var entry2 = CreateEntry(Guid.NewGuid(), 1, entry1.EntryHash);

        var block1 = CreateBlock(1, snapshotHash, [entry1], createdAt);
        var block2 = CreateBlock(2, block1.BlockHash, [entry2], createdAt.AddMinutes(1));

        var command = new ChainRecoveryCommand(
            SnapshotHeight: 0,
            SnapshotBlockHash: snapshotHash,
            ReplicatedBlocks: [block1, block2],
            ReplicatedLedgerEntries: [entry1, entry2],
            TraceId: "trace-1",
            CorrelationId: "corr-1",
            Timestamp: createdAt);

        var result = _engine.Execute(IntelligenceContext<ChainRecoveryCommand>.Create(command)).Output!;

        Assert.Equal(2, result.RecoveredBlocks.Count);
        Assert.Equal(2, result.RecoveredEntries.Count);
        Assert.Equal(2, result.RecoveredHeight);
        Assert.Equal(2, result.RecoveredEntryCount);
        Assert.NotEmpty(result.RecoveryHash);
        Assert.Equal("trace-1", result.TraceId);
    }

    [Fact]
    public void Execute_SnapshotMismatch_Throws()
    {
        var snapshotHash = ComputeHash("genesis");
        var wrongSnapshotHash = ComputeHash("wrong");
        var createdAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        var entry1 = CreateEntry(Guid.NewGuid(), 0, ComputeHash("prev-0"));
        var block1 = CreateBlock(1, snapshotHash, [entry1], createdAt);

        var command = new ChainRecoveryCommand(
            SnapshotHeight: 0,
            SnapshotBlockHash: wrongSnapshotHash,
            ReplicatedBlocks: [block1],
            ReplicatedLedgerEntries: [entry1],
            TraceId: "trace-1",
            CorrelationId: "corr-1",
            Timestamp: createdAt);

        Assert.Throws<InvalidOperationException>(() => _engine.Execute(IntelligenceContext<ChainRecoveryCommand>.Create(command)));
    }

    [Fact]
    public void Execute_InvalidBlockLinkage_Throws()
    {
        var snapshotHash = ComputeHash("genesis");
        var createdAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        var entry1 = CreateEntry(Guid.NewGuid(), 0, ComputeHash("prev-0"));
        var entry2 = CreateEntry(Guid.NewGuid(), 1, entry1.EntryHash);

        var block1 = CreateBlock(1, snapshotHash, [entry1], createdAt);

        // Create block2 with wrong previous hash (not linked to block1)
        var wrongPrevHash = ComputeHash("wrong-linkage");
        var block2 = CreateBlock(2, wrongPrevHash, [entry2], createdAt.AddMinutes(1));

        var command = new ChainRecoveryCommand(
            SnapshotHeight: 0,
            SnapshotBlockHash: snapshotHash,
            ReplicatedBlocks: [block1, block2],
            ReplicatedLedgerEntries: [entry1, entry2],
            TraceId: "trace-1",
            CorrelationId: "corr-1",
            Timestamp: createdAt);

        Assert.Throws<InvalidOperationException>(() => _engine.Execute(IntelligenceContext<ChainRecoveryCommand>.Create(command)));
    }

    [Fact]
    public void Execute_RecoveryHashIsDeterministic()
    {
        var snapshotHash = ComputeHash("genesis");
        var createdAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        var entryId = Guid.Parse("00000000-0000-0000-0000-000000000001");
        var entry1 = CreateEntry(entryId, 0, ComputeHash("prev-0"));
        var block1 = CreateBlock(1, snapshotHash, [entry1], createdAt);

        var command = new ChainRecoveryCommand(
            SnapshotHeight: 0,
            SnapshotBlockHash: snapshotHash,
            ReplicatedBlocks: [block1],
            ReplicatedLedgerEntries: [entry1],
            TraceId: "trace-1",
            CorrelationId: "corr-1",
            Timestamp: createdAt);

        var result1 = _engine.Execute(IntelligenceContext<ChainRecoveryCommand>.Create(command)).Output!;
        var result2 = _engine.Execute(IntelligenceContext<ChainRecoveryCommand>.Create(command)).Output!;

        Assert.Equal(result1.RecoveryHash, result2.RecoveryHash);
        Assert.Equal(result1.RecoveredHeight, result2.RecoveredHeight);
        Assert.Equal(result1.RecoveredEntryCount, result2.RecoveredEntryCount);
    }

    [Fact]
    public void Execute_EmptyReplicatedBlocks_ReturnsSnapshotHeight()
    {
        var snapshotHash = ComputeHash("genesis");
        var createdAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        var command = new ChainRecoveryCommand(
            SnapshotHeight: 5,
            SnapshotBlockHash: snapshotHash,
            ReplicatedBlocks: [],
            ReplicatedLedgerEntries: [],
            TraceId: "trace-1",
            CorrelationId: "corr-1",
            Timestamp: createdAt);

        var result = _engine.Execute(IntelligenceContext<ChainRecoveryCommand>.Create(command)).Output!;

        Assert.Empty(result.RecoveredBlocks);
        Assert.Empty(result.RecoveredEntries);
        Assert.Equal(5, result.RecoveredHeight);
        Assert.Equal(0, result.RecoveredEntryCount);
    }
}
