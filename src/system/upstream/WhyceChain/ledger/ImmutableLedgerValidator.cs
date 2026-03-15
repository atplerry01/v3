namespace Whycespace.System.Upstream.WhyceChain.Ledger;

public sealed record LedgerValidationResult(
    bool IsValid,
    IReadOnlyList<string> Issues);

public static class ImmutableLedgerValidator
{
    public static LedgerValidationResult Validate(ImmutableEventLedger ledger)
    {
        var issues = new List<string>();

        if (ledger.CurrentHeight == 0)
            return new LedgerValidationResult(true, issues.AsReadOnly());

        var entries = ledger.Entries;

        ValidateGenesisEntry(entries[0], ledger.GenesisHash, issues);
        ValidateSequenceContinuity(entries, issues);
        ValidateHashChain(entries, issues);
        ValidateEntryHashes(entries, issues);

        return new LedgerValidationResult(issues.Count == 0, issues.AsReadOnly());
    }

    private static void ValidateGenesisEntry(
        ChainLedgerEntry genesis, string genesisHash, List<string> issues)
    {
        if (!string.IsNullOrEmpty(genesis.PreviousEntryHash))
            issues.Add("Genesis entry must have null or empty PreviousEntryHash.");

        if (genesis.EntryHash != genesisHash)
            issues.Add($"Genesis hash mismatch. Ledger reports '{genesisHash}', entry has '{genesis.EntryHash}'.");
    }

    private static void ValidateSequenceContinuity(
        IReadOnlyList<ChainLedgerEntry> entries, List<string> issues)
    {
        for (var i = 0; i < entries.Count; i++)
        {
            if (entries[i].SequenceNumber != i)
                issues.Add($"Sequence gap at index {i}. Expected {i}, got {entries[i].SequenceNumber}.");
        }
    }

    private static void ValidateHashChain(
        IReadOnlyList<ChainLedgerEntry> entries, List<string> issues)
    {
        for (var i = 1; i < entries.Count; i++)
        {
            if (entries[i].PreviousEntryHash != entries[i - 1].EntryHash)
                issues.Add(
                    $"Hash chain broken at sequence {i}. Expected previous hash '{entries[i - 1].EntryHash}', got '{entries[i].PreviousEntryHash}'.");
        }
    }

    private static void ValidateEntryHashes(
        IReadOnlyList<ChainLedgerEntry> entries, List<string> issues)
    {
        for (var i = 0; i < entries.Count; i++)
        {
            if (string.IsNullOrWhiteSpace(entries[i].EntryHash))
                issues.Add($"Entry at sequence {i} has null or empty EntryHash.");
        }
    }
}
