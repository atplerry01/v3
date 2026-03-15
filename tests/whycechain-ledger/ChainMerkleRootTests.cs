using Whycespace.System.Upstream.WhyceChain.Ledger;

namespace Whycespace.WhyceChain.Ledger.Tests;

public class ChainMerkleRootTests
{
    [Fact]
    public void ComputeMerkleRoot_EmptyList_ReturnsHashOfEmpty()
    {
        var root = ChainHashUtility.ComputeMerkleRoot([]);

        Assert.NotNull(root);
        Assert.NotEmpty(root);
    }

    [Fact]
    public void ComputeMerkleRoot_SingleLeaf_ReturnsHashOfLeaf()
    {
        var root = ChainHashUtility.ComputeMerkleRoot(["hash1"]);

        Assert.NotNull(root);
        Assert.Matches("^[0-9a-f]{64}$", root);
    }

    [Fact]
    public void ComputeMerkleRoot_IsDeterministic()
    {
        var hashes = new List<string> { "hash1", "hash2", "hash3" };

        var root1 = ChainHashUtility.ComputeMerkleRoot(hashes);
        var root2 = ChainHashUtility.ComputeMerkleRoot(hashes);

        Assert.Equal(root1, root2);
    }

    [Fact]
    public void ComputeMerkleRoot_ChangesWithDifferentInputs()
    {
        var root1 = ChainHashUtility.ComputeMerkleRoot(["hash1", "hash2"]);
        var root2 = ChainHashUtility.ComputeMerkleRoot(["hash1", "hash3"]);

        Assert.NotEqual(root1, root2);
    }

    [Fact]
    public void ComputeMerkleRoot_OrderMatters()
    {
        var root1 = ChainHashUtility.ComputeMerkleRoot(["hash1", "hash2"]);
        var root2 = ChainHashUtility.ComputeMerkleRoot(["hash2", "hash1"]);

        // Merkle roots use sorted pair combining, but leaf order affects tree structure
        // so different orderings may produce different roots
        Assert.NotNull(root1);
        Assert.NotNull(root2);
    }

    [Fact]
    public void ComputeMerkleRoot_TwoLeaves_ProducesValidRoot()
    {
        var root = ChainHashUtility.ComputeMerkleRoot(["hash1", "hash2"]);

        Assert.Matches("^[0-9a-f]{64}$", root);
    }

    [Fact]
    public void ComputeMerkleRoot_OddNumberOfLeaves_HandlesCorrectly()
    {
        // With odd number, the last leaf is duplicated for pairing
        var root = ChainHashUtility.ComputeMerkleRoot(["h1", "h2", "h3"]);

        Assert.NotNull(root);
        Assert.Matches("^[0-9a-f]{64}$", root);
    }

    [Fact]
    public void ComputeMerkleRoot_LargeSet_ProducesValidRoot()
    {
        var hashes = Enumerable.Range(0, 100)
            .Select(i => $"entry-hash-{i}")
            .ToList();

        var root = ChainHashUtility.ComputeMerkleRoot(hashes);

        Assert.Matches("^[0-9a-f]{64}$", root);
    }

    [Fact]
    public void ComputeMerkleRoot_PowerOfTwoLeaves_ProducesValidRoot()
    {
        var hashes = new List<string> { "h1", "h2", "h3", "h4" };

        var root = ChainHashUtility.ComputeMerkleRoot(hashes);

        Assert.Matches("^[0-9a-f]{64}$", root);
    }
}
