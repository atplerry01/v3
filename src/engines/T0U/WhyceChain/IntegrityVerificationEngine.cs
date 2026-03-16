namespace Whycespace.Engines.T0U.WhyceChain;

using global::System.Security.Cryptography;
using global::System.Text;
using Whycespace.Systems.Upstream.WhyceChain.Models;

public sealed class IntegrityVerificationEngine
{
    private readonly MerkleProofEngine _merkleEngine;

    public IntegrityVerificationEngine(MerkleProofEngine merkleEngine)
    {
        _merkleEngine = merkleEngine;
    }

    public IntegrityVerificationResult Execute(IntegrityVerificationCommand command)
    {
        var (ledgerValid, tamperedEntries) = VerifyLedgerIntegrity(command.LedgerEntries);
        var blockChainValid = VerifyBlockChainIntegrity(command.Blocks);
        var merkleRootValid = VerifyMerkleRoots(command.Blocks);
        var merkleProofValid = command.MerkleProof is not null
            ? _merkleEngine.VerifyProof(command.MerkleProof)
            : true;

        return new IntegrityVerificationResult(
            LedgerValid: ledgerValid,
            BlockChainValid: blockChainValid,
            MerkleRootValid: merkleRootValid,
            MerkleProofValid: merkleProofValid,
            TamperedEntries: tamperedEntries,
            VerificationTimestamp: DateTimeOffset.UtcNow,
            TraceId: command.TraceId);
    }

    private static (bool Valid, IReadOnlyList<long> TamperedIndices) VerifyLedgerIntegrity(
        IReadOnlyList<ChainLedgerEntry> entries)
    {
        if (entries.Count == 0)
            return (true, Array.Empty<long>());

        var tampered = new List<long>();

        for (var i = 0; i < entries.Count; i++)
        {
            var entry = entries[i];

            // Sequence continuity: first entry must reference "genesis"
            if (i == 0)
            {
                if (entry.PreviousHash != "genesis")
                    tampered.Add(i);
            }
            else
            {
                // Previous entry hash linkage
                var previous = entries[i - 1];
                if (entry.PreviousHash != previous.PayloadHash)
                    tampered.Add(i);
            }

            // Entry hash validity: PayloadHash must not be empty
            if (string.IsNullOrEmpty(entry.PayloadHash))
                tampered.Add(i);
        }

        return (tampered.Count == 0, tampered);
    }

    private static bool VerifyBlockChainIntegrity(IReadOnlyList<ChainBlock> blocks)
    {
        if (blocks.Count == 0)
            return true;

        var sorted = blocks.OrderBy(b => b.BlockNumber).ToList();

        for (var i = 0; i < sorted.Count; i++)
        {
            var block = sorted[i];

            // Block height continuity
            if (block.BlockNumber != i)
                return false;

            // Genesis block must reference "genesis"
            if (i == 0)
            {
                if (block.PreviousBlockHash != "genesis")
                    return false;
            }
            else
            {
                // Previous block hash linkage
                var previous = sorted[i - 1];
                if (block.PreviousBlockHash != previous.BlockHash)
                    return false;
            }

            // Block hash determinism: recompute and compare
            var recomputed = ComputeBlockHash(
                block.BlockNumber,
                block.PreviousBlockHash,
                block.MerkleRoot,
                block.Timestamp);

            if (recomputed != block.BlockHash)
                return false;
        }

        return true;
    }

    private bool VerifyMerkleRoots(IReadOnlyList<ChainBlock> blocks)
    {
        if (blocks.Count == 0)
            return true;

        foreach (var block in blocks)
        {
            var recomputed = _merkleEngine.BuildTree(block.EntryIds);
            if (recomputed != block.MerkleRoot)
                return false;
        }

        return true;
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
