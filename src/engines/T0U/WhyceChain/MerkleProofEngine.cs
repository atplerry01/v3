namespace Whycespace.Engines.T0U.WhyceChain;

using global::System.Security.Cryptography;
using global::System.Text;
using Whycespace.Systems.Upstream.WhyceChain.Models;

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

    public MerkleProofResult GenerateProof(MerkleProofCommand command)
    {
        var entries = command.BlockEntries.ToList();
        var entryIndex = entries.IndexOf(command.EntryHash);
        if (entryIndex < 0)
        {
            return new MerkleProofResult(
                ProofPath: Array.Empty<string>(),
                ComputedRoot: string.Empty,
                ProofValid: false,
                ProofDepth: 0,
                GeneratedAt: DateTime.UtcNow,
                TraceId: command.TraceId);
        }

        var proof = GenerateProof(command.BlockEntries, entryIndex);
        var isValid = proof.RootHash == command.MerkleRoot;

        return new MerkleProofResult(
            ProofPath: proof.ProofPath,
            ComputedRoot: proof.RootHash,
            ProofValid: isValid,
            ProofDepth: proof.ProofPath.Count,
            GeneratedAt: DateTime.UtcNow,
            TraceId: command.TraceId);
    }

    public MerkleProofResult VerifyProof(MerkleProofCommand command, IReadOnlyList<string> proofPath)
    {
        var leafHash = ComputeHash(command.EntryHash);
        var current = leafHash;

        foreach (var sibling in proofPath)
        {
            current = CombineHashes(current, sibling);
        }

        var isValid = current == command.MerkleRoot;

        return new MerkleProofResult(
            ProofPath: proofPath,
            ComputedRoot: current,
            ProofValid: isValid,
            ProofDepth: proofPath.Count,
            GeneratedAt: DateTime.UtcNow,
            TraceId: command.TraceId);
    }

    public IReadOnlyList<IReadOnlyList<string>> InspectTree(IReadOnlyList<string> leafHashes)
    {
        if (leafHashes.Count == 0)
            return Array.Empty<IReadOnlyList<string>>();

        var levels = new List<IReadOnlyList<string>>();
        var level = leafHashes.Select(ComputeHash).ToList();
        levels.Add(level.AsReadOnly());

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
            levels.Add(level.AsReadOnly());
        }

        return levels.AsReadOnly();
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
