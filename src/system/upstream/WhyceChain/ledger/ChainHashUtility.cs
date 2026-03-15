namespace Whycespace.System.Upstream.WhyceChain.Ledger;

using global::System.Security.Cryptography;
using global::System.Text;

public static class ChainHashUtility
{
    public static string ComputeBlockHash(
        long blockHeight,
        string? previousBlockHash,
        string merkleRoot,
        int entryCount,
        DateTime createdAt)
    {
        var input = $"{blockHeight}:{previousBlockHash ?? "null"}:{merkleRoot}:{entryCount}:{createdAt:O}";
        return Convert.ToHexString(
            SHA256.HashData(Encoding.UTF8.GetBytes(input))).ToLowerInvariant();
    }

    public static string ComputeEntryHash(string entryId, string payloadHash, string previousHash)
    {
        var input = $"{entryId}:{payloadHash}:{previousHash}";
        return Convert.ToHexString(
            SHA256.HashData(Encoding.UTF8.GetBytes(input))).ToLowerInvariant();
    }

    public static string ComputeMerkleRoot(IReadOnlyList<string> leafHashes)
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

    private static string CombineHashes(string a, string b)
    {
        var combined = string.CompareOrdinal(a, b) <= 0 ? a + b : b + a;
        return ComputeHash(combined);
    }

    /// <summary>
    /// Generates a deterministic entry hash from canonical ledger entry fields.
    /// </summary>
    public static string GenerateEntryHash(
        Guid entryId,
        string entryType,
        string aggregateId,
        long sequenceNumber,
        string payloadHash,
        string metadataHash,
        string previousEntryHash,
        DateTimeOffset timestamp,
        string traceId,
        string correlationId,
        int eventVersion)
    {
        var input = string.Join(":",
            entryId.ToString("D"),
            entryType,
            aggregateId,
            sequenceNumber.ToString(),
            payloadHash,
            metadataHash,
            previousEntryHash,
            timestamp.ToString("O"),
            traceId,
            correlationId,
            eventVersion.ToString());

        return ComputeHash(input);
    }

    /// <summary>
    /// Generates a deterministic block hash from canonical ledger block fields.
    /// </summary>
    public static string GenerateBlockHash(
        Guid blockId,
        long blockHeight,
        string merkleRoot,
        string previousBlockHash,
        int entryCount,
        DateTimeOffset createdAt)
    {
        var input = string.Join(":",
            blockId.ToString("D"),
            blockHeight.ToString(),
            merkleRoot,
            previousBlockHash,
            entryCount.ToString(),
            createdAt.ToString("O"));

        return ComputeHash(input);
    }

    private static string ComputeHash(string input)
    {
        return Convert.ToHexString(
            SHA256.HashData(Encoding.UTF8.GetBytes(input))).ToLowerInvariant();
    }
}
