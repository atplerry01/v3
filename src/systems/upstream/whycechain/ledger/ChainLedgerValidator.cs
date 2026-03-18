namespace Whycespace.Systems.Upstream.WhyceChain.Ledger;

/// <summary>
/// Result of ledger validation containing validity status and any issues found.
/// </summary>
public sealed record ChainLedgerValidationResult(
    bool IsValid,
    IReadOnlyList<string> Issues);

/// <summary>
/// Validates canonical ChainLedgerEntry and ChainLedgerBlock structures.
/// Enforces sequence continuity, hash chain integrity, deterministic hashing,
/// and non-null payload hashes.
/// </summary>
public static class ChainLedgerValidator
{
    public static ChainLedgerValidationResult ValidateEntry(
        ChainLedgerEntry entry, ChainLedgerEntry? previousEntry = null)
    {
        var issues = new List<string>();

        if (string.IsNullOrWhiteSpace(entry.PayloadHash))
            issues.Add("PayloadHash must not be null or empty.");

        if (string.IsNullOrWhiteSpace(entry.EntryType))
            issues.Add("EntryType must not be null or empty.");

        if (string.IsNullOrWhiteSpace(entry.AggregateId))
            issues.Add("AggregateId must not be null or empty.");

        ValidateEntryHash(entry, issues);

        if (previousEntry is not null)
        {
            ValidateSequenceContinuity(entry, previousEntry, issues);
            ValidatePreviousEntryHash(entry, previousEntry, issues);
        }

        return new ChainLedgerValidationResult(issues.Count == 0, issues);
    }

    public static ChainLedgerValidationResult ValidateEntrySequence(
        IReadOnlyList<ChainLedgerEntry> entries)
    {
        var issues = new List<string>();

        if (entries.Count == 0)
            return new ChainLedgerValidationResult(true, issues);

        // Validate first entry
        if (string.IsNullOrWhiteSpace(entries[0].PayloadHash))
            issues.Add("Entry at sequence 0 has null or empty PayloadHash.");

        ValidateEntryHash(entries[0], issues);

        // Validate chain
        for (var i = 1; i < entries.Count; i++)
        {
            if (string.IsNullOrWhiteSpace(entries[i].PayloadHash))
                issues.Add($"Entry at sequence {i} has null or empty PayloadHash.");

            ValidateSequenceContinuity(entries[i], entries[i - 1], issues);
            ValidatePreviousEntryHash(entries[i], entries[i - 1], issues);
            ValidateEntryHash(entries[i], issues);
        }

        return new ChainLedgerValidationResult(issues.Count == 0, issues);
    }

    public static ChainLedgerValidationResult ValidateBlock(
        ChainLedgerBlock block, ChainLedgerBlock? previousBlock = null)
    {
        var issues = new List<string>();

        if (block.Entries.Count == 0)
            issues.Add("Block must contain at least one entry.");

        ValidateBlockEntryOrdering(block, issues);
        ValidateBlockMerkleRoot(block, issues);
        ValidateBlockHash(block, issues);

        if (previousBlock is not null)
            ValidateBlockChainLink(block, previousBlock, issues);

        return new ChainLedgerValidationResult(issues.Count == 0, issues);
    }

    private static void ValidateEntryHash(ChainLedgerEntry entry, List<string> issues)
    {
        var expectedHash = ChainHashUtility.GenerateEntryHash(
            entry.EntryId,
            entry.EntryType,
            entry.AggregateId,
            entry.SequenceNumber,
            entry.PayloadHash,
            entry.MetadataHash,
            entry.PreviousEntryHash,
            entry.Timestamp,
            entry.TraceId,
            entry.CorrelationId,
            entry.EventVersion);

        if (entry.EntryHash != expectedHash)
            issues.Add($"EntryHash mismatch at sequence {entry.SequenceNumber}. " +
                        $"Expected '{expectedHash}', got '{entry.EntryHash}'.");
    }

    private static void ValidateSequenceContinuity(
        ChainLedgerEntry current, ChainLedgerEntry previous, List<string> issues)
    {
        if (current.SequenceNumber != previous.SequenceNumber + 1)
            issues.Add($"Sequence gap: expected {previous.SequenceNumber + 1}, " +
                        $"got {current.SequenceNumber}.");
    }

    private static void ValidatePreviousEntryHash(
        ChainLedgerEntry current, ChainLedgerEntry previous, List<string> issues)
    {
        if (current.PreviousEntryHash != previous.EntryHash)
            issues.Add($"Previous entry hash mismatch at sequence {current.SequenceNumber}. " +
                        $"Expected '{previous.EntryHash}', got '{current.PreviousEntryHash}'.");
    }

    private static void ValidateBlockEntryOrdering(ChainLedgerBlock block, List<string> issues)
    {
        for (var i = 1; i < block.Entries.Count; i++)
        {
            if (block.Entries[i].SequenceNumber <= block.Entries[i - 1].SequenceNumber)
            {
                issues.Add($"Entries not ordered by sequence at index {i}.");
                break;
            }
        }
    }

    private static void ValidateBlockMerkleRoot(ChainLedgerBlock block, List<string> issues)
    {
        if (block.Entries.Count == 0)
            return;

        var entryHashes = block.Entries.Select(e => e.EntryHash).ToList();
        var expectedRoot = ChainHashUtility.ComputeMerkleRoot(entryHashes);

        if (block.MerkleRoot != expectedRoot)
            issues.Add("MerkleRoot does not match computed value from entry hashes.");
    }

    private static void ValidateBlockHash(ChainLedgerBlock block, List<string> issues)
    {
        var expectedHash = ChainHashUtility.GenerateBlockHash(
            block.BlockId,
            block.BlockHeight,
            block.MerkleRoot,
            block.PreviousBlockHash,
            block.Entries.Count,
            block.CreatedAt);

        if (block.BlockHash != expectedHash)
            issues.Add("BlockHash does not match recomputed deterministic hash.");
    }

    private static void ValidateBlockChainLink(
        ChainLedgerBlock current, ChainLedgerBlock previous, List<string> issues)
    {
        if (current.PreviousBlockHash != previous.BlockHash)
            issues.Add("PreviousBlockHash does not match the previous block's BlockHash.");

        if (current.BlockHeight != previous.BlockHeight + 1)
            issues.Add($"Block height gap: expected {previous.BlockHeight + 1}, " +
                        $"got {current.BlockHeight}.");
    }
}
