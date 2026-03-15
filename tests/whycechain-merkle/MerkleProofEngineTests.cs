using Whycespace.Engines.T0U.WhyceChain;
using Whycespace.System.Upstream.WhyceChain.Models;

namespace Whycespace.WhyceChainMerkle.Tests;

public class MerkleProofEngineTests
{
    private readonly MerkleProofEngine _engine = new();

    [Fact]
    public void BuildTree_ShouldReturnDeterministicRoot()
    {
        var leaves = new[] { "hash-1", "hash-2", "hash-3", "hash-4" };

        var first = _engine.BuildTree(leaves);
        var second = _engine.BuildTree(leaves);

        Assert.Equal(first, second);
        Assert.NotEmpty(first);
    }

    [Fact]
    public void BuildTree_DifferentInputs_ProduceDifferentRoots()
    {
        var leaves1 = new[] { "hash-1", "hash-2", "hash-3", "hash-4" };
        var leaves2 = new[] { "hash-1", "hash-2", "hash-3", "hash-5" };

        var root1 = _engine.BuildTree(leaves1);
        var root2 = _engine.BuildTree(leaves2);

        Assert.NotEqual(root1, root2);
    }

    [Fact]
    public void GenerateProof_ShouldProduceValidProof()
    {
        var leaves = new[] { "hash-1", "hash-2", "hash-3", "hash-4" };

        var proof = _engine.GenerateProof(leaves, 0);

        Assert.Equal(_engine.BuildTree(leaves), proof.RootHash);
        Assert.NotEmpty(proof.ProofPath);
    }

    [Fact]
    public void VerifyProof_ShouldConfirmValidProof()
    {
        var leaves = new[] { "hash-1", "hash-2", "hash-3", "hash-4" };

        for (var i = 0; i < leaves.Length; i++)
        {
            var proof = _engine.GenerateProof(leaves, i);
            Assert.True(_engine.VerifyProof(proof));
        }
    }

    [Fact]
    public void VerifyProof_TamperedProof_ShouldFail()
    {
        var leaves = new[] { "hash-1", "hash-2", "hash-3", "hash-4" };
        var proof = _engine.GenerateProof(leaves, 0);

        var tampered = new MerkleProof(proof.RootHash, "tampered-leaf-hash", proof.ProofPath);

        Assert.False(_engine.VerifyProof(tampered));
    }

    [Fact]
    public void BuildTree_OddNodeCount_ShouldDuplicateLastNode()
    {
        var leaves = new[] { "hash-1", "hash-2", "hash-3" };

        var root = _engine.BuildTree(leaves);

        Assert.NotEmpty(root);

        var second = _engine.BuildTree(leaves);
        Assert.Equal(root, second);
    }

    [Fact]
    public void GenerateProof_OddNodeCount_ShouldProduceValidProof()
    {
        var leaves = new[] { "hash-1", "hash-2", "hash-3" };

        for (var i = 0; i < leaves.Length; i++)
        {
            var proof = _engine.GenerateProof(leaves, i);
            Assert.True(_engine.VerifyProof(proof));
        }
    }

    [Fact]
    public void GenerateProof_SingleLeaf_ShouldProduceValidProof()
    {
        var leaves = new[] { "hash-1" };

        var proof = _engine.GenerateProof(leaves, 0);
        Assert.True(_engine.VerifyProof(proof));
        Assert.Empty(proof.ProofPath);
    }

    [Fact]
    public void GenerateProof_Command_ShouldReturnValidResult()
    {
        var entries = new[] { "entry-1", "entry-2", "entry-3", "entry-4" };
        var merkleRoot = _engine.BuildTree(entries);

        var command = new MerkleProofCommand(
            EntryHash: "entry-1",
            BlockEntries: entries,
            MerkleRoot: merkleRoot,
            TraceId: "trace-1",
            CorrelationId: "corr-1",
            Timestamp: DateTime.UtcNow);

        var result = _engine.GenerateProof(command);

        Assert.True(result.ProofValid);
        Assert.Equal(merkleRoot, result.ComputedRoot);
        Assert.NotEmpty(result.ProofPath);
        Assert.True(result.ProofDepth > 0);
        Assert.Equal("trace-1", result.TraceId);
    }

    [Fact]
    public void GenerateProof_Command_EntryNotFound_ShouldReturnInvalid()
    {
        var entries = new[] { "entry-1", "entry-2", "entry-3", "entry-4" };
        var merkleRoot = _engine.BuildTree(entries);

        var command = new MerkleProofCommand(
            EntryHash: "nonexistent-entry",
            BlockEntries: entries,
            MerkleRoot: merkleRoot,
            TraceId: "trace-2",
            CorrelationId: "corr-2",
            Timestamp: DateTime.UtcNow);

        var result = _engine.GenerateProof(command);

        Assert.False(result.ProofValid);
        Assert.Empty(result.ProofPath);
    }

    [Fact]
    public void VerifyProof_Command_ShouldValidateExistingProof()
    {
        var entries = new[] { "entry-1", "entry-2", "entry-3", "entry-4" };
        var merkleRoot = _engine.BuildTree(entries);

        var generateCommand = new MerkleProofCommand(
            EntryHash: "entry-2",
            BlockEntries: entries,
            MerkleRoot: merkleRoot,
            TraceId: "trace-3",
            CorrelationId: "corr-3",
            Timestamp: DateTime.UtcNow);

        var generated = _engine.GenerateProof(generateCommand);

        var verifyCommand = new MerkleProofCommand(
            EntryHash: "entry-2",
            BlockEntries: entries,
            MerkleRoot: merkleRoot,
            TraceId: "trace-4",
            CorrelationId: "corr-4",
            Timestamp: DateTime.UtcNow);

        var verified = _engine.VerifyProof(verifyCommand, generated.ProofPath);

        Assert.True(verified.ProofValid);
        Assert.Equal(merkleRoot, verified.ComputedRoot);
    }

    [Fact]
    public void VerifyProof_Command_WrongMerkleRoot_ShouldFail()
    {
        var entries = new[] { "entry-1", "entry-2", "entry-3", "entry-4" };
        var merkleRoot = _engine.BuildTree(entries);

        var generateCommand = new MerkleProofCommand(
            EntryHash: "entry-1",
            BlockEntries: entries,
            MerkleRoot: merkleRoot,
            TraceId: "trace-5",
            CorrelationId: "corr-5",
            Timestamp: DateTime.UtcNow);

        var generated = _engine.GenerateProof(generateCommand);

        var verifyCommand = new MerkleProofCommand(
            EntryHash: "entry-1",
            BlockEntries: entries,
            MerkleRoot: "wrong-root",
            TraceId: "trace-6",
            CorrelationId: "corr-6",
            Timestamp: DateTime.UtcNow);

        var verified = _engine.VerifyProof(verifyCommand, generated.ProofPath);

        Assert.False(verified.ProofValid);
    }

    [Fact]
    public void InspectTree_ShouldReturnAllLevels()
    {
        var leaves = new[] { "hash-1", "hash-2", "hash-3", "hash-4" };

        var levels = _engine.InspectTree(leaves);

        Assert.Equal(3, levels.Count); // 4 leaves -> 2 internal -> 1 root
        Assert.Equal(4, levels[0].Count);
        Assert.Equal(2, levels[1].Count);
        Assert.Single(levels[2]);
        Assert.Equal(_engine.BuildTree(leaves), levels[2][0]);
    }

    [Fact]
    public void ConcurrencySafety_ParallelProofGeneration()
    {
        var leaves = new[] { "hash-1", "hash-2", "hash-3", "hash-4" };
        var expectedRoot = _engine.BuildTree(leaves);

        Parallel.For(0, 100, _ =>
        {
            for (var i = 0; i < leaves.Length; i++)
            {
                var proof = _engine.GenerateProof(leaves, i);
                Assert.True(_engine.VerifyProof(proof));
                Assert.Equal(expectedRoot, proof.RootHash);
            }
        });
    }

    [Fact]
    public void LargeTree_Performance()
    {
        var leaves = Enumerable.Range(0, 1000).Select(i => $"hash-{i}").ToArray();

        var root = _engine.BuildTree(leaves);
        Assert.NotEmpty(root);

        var proof = _engine.GenerateProof(leaves, 500);
        Assert.True(_engine.VerifyProof(proof));
        Assert.Equal(root, proof.RootHash);
    }
}
