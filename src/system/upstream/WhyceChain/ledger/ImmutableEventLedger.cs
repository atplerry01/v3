namespace Whycespace.System.Upstream.WhyceChain.Ledger;

public sealed class ImmutableEventLedger
{
    private readonly List<ChainLedgerEntry> _entries = new();
    private readonly object _lock = new();

    public Guid LedgerId { get; }
    public long CurrentHeight => _entries.Count;
    public IReadOnlyList<ChainLedgerEntry> Entries => _entries.AsReadOnly();
    public string GenesisHash { get; private set; } = string.Empty;
    public string LatestEntryHash => _entries.Count > 0 ? _entries[^1].EntryHash : string.Empty;
    public DateTime CreatedAt { get; }
    public string TraceId { get; }

    public ImmutableEventLedger(Guid ledgerId, string traceId)
    {
        LedgerId = ledgerId;
        TraceId = traceId;
        CreatedAt = DateTime.UtcNow;
    }

    public ImmutableEventLedger AppendEntry(ChainLedgerEntry entry)
    {
        lock (_lock)
        {
            var expectedSequence = _entries.Count;

            if (entry.SequenceNumber != expectedSequence)
                throw new InvalidOperationException(
                    $"Sequence mismatch. Expected {expectedSequence}, got {entry.SequenceNumber}.");

            if (expectedSequence == 0)
            {
                if (!string.IsNullOrEmpty(entry.PreviousEntryHash))
                    throw new InvalidOperationException("Genesis entry must have null or empty PreviousEntryHash.");

                _entries.Add(entry);
                GenesisHash = entry.EntryHash;
                return this;
            }

            var lastEntry = _entries[^1];

            if (entry.PreviousEntryHash != lastEntry.EntryHash)
                throw new InvalidOperationException(
                    $"Previous hash mismatch. Expected '{lastEntry.EntryHash}', got '{entry.PreviousEntryHash}'.");

            if (string.IsNullOrWhiteSpace(entry.EntryHash))
                throw new InvalidOperationException("EntryHash must not be null or empty.");

            _entries.Add(entry);
            return this;
        }
    }

    public ChainLedgerEntry GetEntry(long sequenceNumber)
    {
        if (sequenceNumber < 0 || sequenceNumber >= _entries.Count)
            throw new ArgumentOutOfRangeException(nameof(sequenceNumber),
                $"Sequence number {sequenceNumber} is out of range. Ledger height: {_entries.Count}.");

        return _entries[(int)sequenceNumber];
    }

    public ChainLedgerEntry GetLatestEntry()
    {
        if (_entries.Count == 0)
            throw new InvalidOperationException("Ledger is empty.");

        return _entries[^1];
    }

    public IReadOnlyList<ChainLedgerEntry> GetEntriesRange(long startSequence, long endSequence)
    {
        if (startSequence < 0)
            throw new ArgumentOutOfRangeException(nameof(startSequence), "Start sequence must be non-negative.");

        if (endSequence < startSequence)
            throw new ArgumentOutOfRangeException(nameof(endSequence), "End sequence must be >= start sequence.");

        if (endSequence >= _entries.Count)
            throw new ArgumentOutOfRangeException(nameof(endSequence),
                $"End sequence {endSequence} exceeds ledger height {_entries.Count}.");

        return _entries.GetRange((int)startSequence, (int)(endSequence - startSequence + 1)).AsReadOnly();
    }
}
