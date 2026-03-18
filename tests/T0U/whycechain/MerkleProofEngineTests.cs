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

namespace Whycespace.WhyceChain.Tests;

public class MerkleProofEngineTests
{
    private readonly MerkleProofEngine _engine;

    public MerkleProofEngineTests()
    {
        _engine = new MerkleProofEngine();
    }

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
}
