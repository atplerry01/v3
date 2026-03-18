using System.Security.Cryptography;
using System.Text;
using Whycespace.Engines.T0U.WhyceChain.Block.Builder;
using Whycespace.Engines.T0U.WhyceChain.Block.Anchor;
using Whycespace.Engines.T0U.WhyceChain.Ledger.Event;
using Whycespace.Engines.T0U.WhyceChain.Ledger.Immutable;
using Whycespace.Engines.T0U.WhyceChain.Ledger.Indexing;
using Whycespace.Engines.T0U.WhyceChain.Verification.Integrity;
using Whycespace.Engines.T0U.WhyceChain.Verification.Merkle;
using Whycespace.Engines.T0U.WhyceChain.Verification.Audit;
using Whycespace.Engines.T0U.WhyceChain.Replication.Replication;
using Whycespace.Engines.T0U.WhyceChain.Replication.Snapshot;
using Whycespace.Engines.T0U.WhyceChain.Append.Execution;
using Whycespace.Engines.T0U.WhyceChain.Evidence.Hashing;
using Whycespace.Engines.T0U.WhyceChain.Evidence.Anchoring;
using Whycespace.Engines.T0U.WhyceChain.Evidence.Gateway;
using Whycespace.Systems.Upstream.WhyceChain.Models;

namespace Whycespace.WhyceChainIntegrity.Tests;

public class IntegrityVerificationEngineTests
{
    private readonly MerkleProofEngine _merkleEngine;
    private readonly IntegrityVerificationEngine _engine;

    public IntegrityVerificationEngineTests()
    {
        _merkleEngine = new MerkleProofEngine();
        _engine = new IntegrityVerificationEngine(_merkleEngine);
    }

    // ── Ledger Validation ──

    [Fact]
    public void Execute_ValidLedger_ShouldReportLedgerValid()
    {
        var entries = BuildValidLedgerEntries(5);
        var command = CreateCommand(entries, []);

        var result = _engine.Execute(command);

        Assert.True(result.LedgerValid);
        Assert.Empty(result.TamperedEntries);
    }

    [Fact]
    public void Execute_EmptyLedger_ShouldReportLedgerValid()
    {
        var command = CreateCommand([], []);

        var result = _engine.Execute(command);

        Assert.True(result.LedgerValid);
        Assert.Empty(result.TamperedEntries);
    }

    [Fact]
    public void Execute_TamperedEntry_ShouldDetectTampering()
    {
        var entries = BuildValidLedgerEntries(3);
        // Tamper with second entry's PreviousHash
        var tampered = entries[1] with { PreviousHash = "tampered-hash" };
        var withTampered = new List<ChainLedgerEntry> { entries[0], tampered, entries[2] };

        var command = CreateCommand(withTampered, []);

        var result = _engine.Execute(command);

        Assert.False(result.LedgerValid);
        Assert.Contains(1L, result.TamperedEntries);
    }

    [Fact]
    public void Execute_BrokenGenesisLink_ShouldDetectTampering()
    {
        var entry = new ChainLedgerEntry(
            "entry-0",
            DateTimeOffset.UtcNow,
            "test.event",
            "hash-0",
            "not-genesis",
            null);

        var command = CreateCommand([entry], []);

        var result = _engine.Execute(command);

        Assert.False(result.LedgerValid);
        Assert.Contains(0L, result.TamperedEntries);
    }

    [Fact]
    public void Execute_EmptyPayloadHash_ShouldDetectTampering()
    {
        var entry = new ChainLedgerEntry(
            "entry-0",
            DateTimeOffset.UtcNow,
            "test.event",
            "",
            "genesis",
            null);

        var command = CreateCommand([entry], []);

        var result = _engine.Execute(command);

        Assert.False(result.LedgerValid);
        Assert.Contains(0L, result.TamperedEntries);
    }

    // ── Block Chain Validation ──

    [Fact]
    public void Execute_ValidBlockChain_ShouldReportBlockChainValid()
    {
        var blocks = BuildValidBlockChain(3);
        var command = CreateCommand([], blocks);

        var result = _engine.Execute(command);

        Assert.True(result.BlockChainValid);
    }

    [Fact]
    public void Execute_EmptyBlockChain_ShouldReportBlockChainValid()
    {
        var command = CreateCommand([], []);

        var result = _engine.Execute(command);

        Assert.True(result.BlockChainValid);
    }

    [Fact]
    public void Execute_BlockHashMismatch_ShouldDetectInvalidChain()
    {
        var blocks = BuildValidBlockChain(2);
        // Corrupt the block hash of the second block
        var corrupted = blocks[1] with { BlockHash = "corrupted-hash" };
        var withCorrupted = new List<ChainBlock> { blocks[0], corrupted };

        var command = CreateCommand([], withCorrupted);

        var result = _engine.Execute(command);

        Assert.False(result.BlockChainValid);
    }

    [Fact]
    public void Execute_BrokenBlockLinkage_ShouldDetectInvalidChain()
    {
        var blocks = BuildValidBlockChain(2);
        // Break previous block hash linkage on second block
        var broken = blocks[1] with { PreviousBlockHash = "wrong-previous" };
        var withBroken = new List<ChainBlock> { blocks[0], broken };

        var command = CreateCommand([], withBroken);

        var result = _engine.Execute(command);

        Assert.False(result.BlockChainValid);
    }

    [Fact]
    public void Execute_NonSequentialBlockNumbers_ShouldDetectInvalidChain()
    {
        var timestamp = DateTimeOffset.UtcNow;
        var merkleRoot = _merkleEngine.BuildTree(["e1"]);
        var blockHash0 = ComputeBlockHash(0, "genesis", merkleRoot, timestamp);

        var block0 = new ChainBlock("b0", 0, "genesis", blockHash0, merkleRoot, timestamp, ["e1"]);
        // Skip block number 1, go straight to 2
        var blockHash2 = ComputeBlockHash(2, blockHash0, merkleRoot, timestamp);
        var block2 = new ChainBlock("b2", 2, blockHash0, blockHash2, merkleRoot, timestamp, ["e2"]);

        var command = CreateCommand([], [block0, block2]);

        var result = _engine.Execute(command);

        Assert.False(result.BlockChainValid);
    }

    // ── Merkle Root Validation ──

    [Fact]
    public void Execute_ValidMerkleRoots_ShouldReportMerkleRootValid()
    {
        var blocks = BuildValidBlockChain(2);
        var command = CreateCommand([], blocks);

        var result = _engine.Execute(command);

        Assert.True(result.MerkleRootValid);
    }

    [Fact]
    public void Execute_InvalidMerkleRoot_ShouldDetectMerkleRootInvalid()
    {
        var blocks = BuildValidBlockChain(1);
        // Corrupt the merkle root but keep the block hash matching the corrupted root
        var timestamp = blocks[0].Timestamp;
        var wrongMerkle = "wrong-merkle-root";
        var newBlockHash = ComputeBlockHash(0, "genesis", wrongMerkle, timestamp);
        var corrupted = blocks[0] with { MerkleRoot = wrongMerkle, BlockHash = newBlockHash };

        var command = CreateCommand([], [corrupted]);

        var result = _engine.Execute(command);

        Assert.False(result.MerkleRootValid);
    }

    // ── Merkle Proof Validation ──

    [Fact]
    public void Execute_ValidMerkleProof_ShouldReportProofValid()
    {
        var leaves = new List<string> { "leaf-a", "leaf-b", "leaf-c" };
        var proof = _merkleEngine.GenerateProof(leaves, 1);
        var command = CreateCommand([], [], proof);

        var result = _engine.Execute(command);

        Assert.True(result.MerkleProofValid);
    }

    [Fact]
    public void Execute_InvalidMerkleProof_ShouldReportProofInvalid()
    {
        var fakeProof = new MerkleProof("wrong-root", "wrong-leaf", ["sibling-1"]);
        var command = CreateCommand([], [], fakeProof);

        var result = _engine.Execute(command);

        Assert.False(result.MerkleProofValid);
    }

    [Fact]
    public void Execute_NoMerkleProof_ShouldDefaultToValid()
    {
        var command = CreateCommand([], [], null);

        var result = _engine.Execute(command);

        Assert.True(result.MerkleProofValid);
    }

    // ── Combined Validation ──

    [Fact]
    public void Execute_FullValidChain_ShouldPassAllChecks()
    {
        var entries = BuildValidLedgerEntries(4);
        var blocks = BuildValidBlockChain(2);
        var leaves = new List<string> { "a", "b" };
        var proof = _merkleEngine.GenerateProof(leaves, 0);

        var command = CreateCommand(entries, blocks, proof);

        var result = _engine.Execute(command);

        Assert.True(result.LedgerValid);
        Assert.True(result.BlockChainValid);
        Assert.True(result.MerkleRootValid);
        Assert.True(result.MerkleProofValid);
        Assert.Empty(result.TamperedEntries);
        Assert.Equal("trace-1", result.TraceId);
    }

    [Fact]
    public void Execute_ShouldBeDeterministic()
    {
        var entries = BuildValidLedgerEntries(3);
        var blocks = BuildValidBlockChain(2);
        var command = CreateCommand(entries, blocks);

        var result1 = _engine.Execute(command);
        var result2 = _engine.Execute(command);

        Assert.Equal(result1.LedgerValid, result2.LedgerValid);
        Assert.Equal(result1.BlockChainValid, result2.BlockChainValid);
        Assert.Equal(result1.MerkleRootValid, result2.MerkleRootValid);
        Assert.Equal(result1.MerkleProofValid, result2.MerkleProofValid);
        Assert.Equal(result1.TamperedEntries, result2.TamperedEntries);
    }

    // ── Helpers ──

    private static IntegrityVerificationCommand CreateCommand(
        IReadOnlyList<ChainLedgerEntry> entries,
        IReadOnlyList<ChainBlock> blocks,
        MerkleProof? proof = null)
    {
        return new IntegrityVerificationCommand(
            entries,
            blocks,
            proof,
            "trace-1",
            "correlation-1",
            DateTimeOffset.UtcNow);
    }

    private static List<ChainLedgerEntry> BuildValidLedgerEntries(int count)
    {
        var entries = new List<ChainLedgerEntry>();
        var previousHash = "genesis";

        for (var i = 0; i < count; i++)
        {
            var payloadHash = $"hash-{i}";
            entries.Add(new ChainLedgerEntry(
                $"entry-{i}",
                DateTimeOffset.UtcNow,
                "test.event",
                payloadHash,
                previousHash,
                null));
            previousHash = payloadHash;
        }

        return entries;
    }

    private List<ChainBlock> BuildValidBlockChain(int count)
    {
        var blocks = new List<ChainBlock>();
        var previousBlockHash = "genesis";

        for (var i = 0; i < count; i++)
        {
            var entryIds = new List<string> { $"entry-{i * 2}", $"entry-{i * 2 + 1}" };
            var merkleRoot = _merkleEngine.BuildTree(entryIds);
            var timestamp = DateTimeOffset.UtcNow;
            var blockHash = ComputeBlockHash(i, previousBlockHash, merkleRoot, timestamp);

            blocks.Add(new ChainBlock(
                $"block-{i}",
                i,
                previousBlockHash,
                blockHash,
                merkleRoot,
                timestamp,
                entryIds));

            previousBlockHash = blockHash;
        }

        return blocks;
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
