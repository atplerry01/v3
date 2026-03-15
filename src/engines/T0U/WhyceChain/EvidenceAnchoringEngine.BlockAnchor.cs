namespace Whycespace.Engines.T0U.WhyceChain;

using global::System.Security.Cryptography;
using global::System.Text;
using global::System.Text.Json;
using Whycespace.System.Upstream.WhyceChain.Ledger;
using LedgerChainBlock = Whycespace.System.Upstream.WhyceChain.Ledger.ChainBlock;

public sealed partial class EvidenceAnchoringEngine
{
    private static readonly JsonSerializerOptions CanonicalJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
        DefaultIgnoreCondition = global::System.Text.Json.Serialization.JsonIgnoreCondition.Never
    };

    public EvidenceAnchorResult Execute(EvidenceAnchorCommand command)
    {
        var block = command.Block;

        if (string.IsNullOrEmpty(block.MerkleRoot))
            throw new ArgumentException("Block must contain a MerkleRoot.");

        if (string.IsNullOrEmpty(block.BlockHash))
            throw new ArgumentException("Block must contain a BlockHash.");

        if (string.IsNullOrEmpty(command.AnchorTarget))
            throw new ArgumentException("AnchorTarget is required.");

        var anchorPayload = BuildCanonicalPayload(block);
        var anchorPayloadHash = ComputeSha256(anchorPayload);
        var anchorReferenceId = GenerateAnchorReferenceId(block, command.AnchorTarget);

        return new EvidenceAnchorResult(
            BlockHash: block.BlockHash,
            MerkleRoot: block.MerkleRoot,
            AnchorPayload: anchorPayload,
            AnchorPayloadHash: anchorPayloadHash,
            AnchorTarget: command.AnchorTarget,
            AnchorReferenceId: anchorReferenceId,
            GeneratedAt: command.Timestamp,
            TraceId: command.TraceId);
    }

    private static string BuildCanonicalPayload(LedgerChainBlock block)
    {
        var payload = new SortedDictionary<string, object>(StringComparer.Ordinal)
        {
            ["blockHash"] = block.BlockHash,
            ["blockHeight"] = block.BlockHeight,
            ["createdAt"] = block.CreatedAt.ToString("O"),
            ["entryCount"] = block.EntryCount,
            ["merkleRoot"] = block.MerkleRoot
        };

        return JsonSerializer.Serialize(payload, CanonicalJsonOptions);
    }

    private static string ComputeSha256(string input)
    {
        return Convert.ToBase64String(
            SHA256.HashData(Encoding.UTF8.GetBytes(input)));
    }

    private static string GenerateAnchorReferenceId(LedgerChainBlock block, string anchorTarget)
    {
        var raw = $"{anchorTarget}:{block.BlockHeight}:{block.BlockHash}";
        return ComputeSha256(raw);
    }
}
