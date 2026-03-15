namespace Whycespace.System.Upstream.WhyceChain.Ledger;

/// <summary>
/// Validates WhyceChain block integrity including entry ordering,
/// hash determinism, Merkle root consistency, and chain linking.
/// </summary>
public sealed class ChainBlockValidator
{
    public ChainBlockValidationResult Validate(ChainBlock block, ChainBlock? previousBlock = null)
    {
        var errors = new List<string>();

        ValidateEntries(block, errors);
        ValidateEntryOrdering(block, errors);
        ValidateEntryCount(block, errors);
        ValidateMerkleRoot(block, errors);
        ValidateBlockHash(block, errors);
        ValidateChainLink(block, previousBlock, errors);

        return new ChainBlockValidationResult(errors.Count == 0, errors);
    }

    private static void ValidateEntries(ChainBlock block, List<string> errors)
    {
        if (block.Entries.Count == 0)
            errors.Add("Block must contain at least one entry.");
    }

    private static void ValidateEntryOrdering(ChainBlock block, List<string> errors)
    {
        for (var i = 1; i < block.Entries.Count; i++)
        {
            if (block.Entries[i].Timestamp < block.Entries[i - 1].Timestamp)
            {
                errors.Add($"Entries are not ordered: entry at index {i} has earlier timestamp than entry at index {i - 1}.");
                break;
            }
        }
    }

    private static void ValidateEntryCount(ChainBlock block, List<string> errors)
    {
        if (block.EntryCount != block.Entries.Count)
            errors.Add($"EntryCount ({block.EntryCount}) does not match actual entry count ({block.Entries.Count}).");
    }

    private static void ValidateMerkleRoot(ChainBlock block, List<string> errors)
    {
        if (block.Entries.Count == 0)
            return;

        var entryHashes = block.Entries.Select(e => e.EntryHash).ToList();
        var expectedRoot = ChainHashUtility.ComputeMerkleRoot(entryHashes);

        if (block.MerkleRoot != expectedRoot)
            errors.Add("MerkleRoot does not match computed value from entry hashes.");
    }

    private static void ValidateBlockHash(ChainBlock block, List<string> errors)
    {
        var expectedHash = ChainHashUtility.ComputeBlockHash(
            block.BlockHeight,
            block.PreviousBlockHash,
            block.MerkleRoot,
            block.EntryCount,
            block.CreatedAt);

        if (block.BlockHash != expectedHash)
            errors.Add("BlockHash is not deterministic — does not match recomputed hash.");
    }

    private static void ValidateChainLink(ChainBlock block, ChainBlock? previousBlock, List<string> errors)
    {
        if (block.BlockHeight == 0)
        {
            if (block.PreviousBlockHash is not null)
                errors.Add("Genesis block must have null PreviousBlockHash.");
            return;
        }

        if (string.IsNullOrEmpty(block.PreviousBlockHash))
            errors.Add("Non-genesis block must have a PreviousBlockHash.");

        if (previousBlock is not null && block.PreviousBlockHash != previousBlock.BlockHash)
            errors.Add("PreviousBlockHash does not match the previous block's BlockHash.");
    }
}

public sealed record ChainBlockValidationResult(
    bool IsValid,
    IReadOnlyList<string> Errors);
