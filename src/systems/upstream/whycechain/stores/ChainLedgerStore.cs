namespace Whycespace.Systems.Upstream.WhyceChain.Stores;

using global::System.Collections.Concurrent;
using Whycespace.Systems.Upstream.WhyceChain.Models;

public sealed class ChainLedgerStore
{
    private readonly ConcurrentDictionary<string, ChainLedgerEntry> _entries = new();

    public void AddEntry(ChainLedgerEntry entry)
    {
        if (!_entries.TryAdd(entry.EntryId, entry))
            throw new InvalidOperationException($"Duplicate entry: {entry.EntryId}");
    }

    public ChainLedgerEntry GetEntry(string entryId)
    {
        if (!_entries.TryGetValue(entryId, out var entry))
            throw new KeyNotFoundException($"Ledger entry not found: {entryId}");

        return entry;
    }

    public IReadOnlyCollection<ChainLedgerEntry> GetAllEntries()
    {
        return _entries.Values.ToList();
    }

    public IReadOnlyCollection<ChainLedgerEntry> GetEntriesByBlock(string blockId)
    {
        return _entries.Values
            .Where(e => e.BlockId == blockId)
            .ToList();
    }

    public void UpdateBlockId(string entryId, string blockId)
    {
        if (!_entries.TryGetValue(entryId, out var entry))
            throw new KeyNotFoundException($"Ledger entry not found: {entryId}");

        _entries[entryId] = entry with { BlockId = blockId };
    }
}
