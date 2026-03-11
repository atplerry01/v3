namespace Whycespace.Engines.T0U.WhyceChain;

using global::System.Security.Cryptography;
using global::System.Text;
using Whycespace.System.Upstream.WhyceChain.Models;

public sealed class MerkleProofEngine
{
    public string BuildTree(IReadOnlyList<string> leafHashes)
    {
        if (leafHashes.Count == 0)
            return ComputeHash("empty");

        var level = leafHashes.Select(ComputeHash).ToList();

        while (level.Count > 1)
        {
            var next = new List<string>();
            for (var i = 0; i < level.Count; i += 2)
            {
                var left = level[i];
                var right = i + 1 < level.Count ? level[i + 1] : left;
                next.Add(CombineHashes(left, right));
            }
            level = next;
        }

        return level[0];
    }

    public MerkleProof GenerateProof(IReadOnlyList<string> leafHashes, int leafIndex)
    {
        if (leafIndex < 0 || leafIndex >= leafHashes.Count)
            throw new ArgumentOutOfRangeException(nameof(leafIndex));

        var leafHash = ComputeHash(leafHashes[leafIndex]);
        var level = leafHashes.Select(ComputeHash).ToList();
        var proofPath = new List<string>();
        var index = leafIndex;

        while (level.Count > 1)
        {
            var next = new List<string>();
            for (var i = 0; i < level.Count; i += 2)
            {
                var left = level[i];
                var right = i + 1 < level.Count ? level[i + 1] : left;

                if (i == index || i + 1 == index)
                {
                    proofPath.Add(i == index ? right : left);
                }

                next.Add(CombineHashes(left, right));
            }
            index /= 2;
            level = next;
        }

        return new MerkleProof(level[0], leafHash, proofPath);
    }

    public bool VerifyProof(MerkleProof proof)
    {
        var current = proof.LeafHash;

        foreach (var sibling in proof.ProofPath)
        {
            current = CombineHashes(current, sibling);
        }

        return current == proof.RootHash;
    }

    private static string CombineHashes(string a, string b)
    {
        var combined = string.CompareOrdinal(a, b) <= 0 ? a + b : b + a;
        return ComputeHash(combined);
    }

    private static string ComputeHash(string input)
    {
        return Convert.ToBase64String(
            SHA256.HashData(Encoding.UTF8.GetBytes(input)));
    }
}
