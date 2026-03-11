namespace Whycespace.System.Upstream.WhyceChain.Models;

public sealed record ChainLedgerEntry(
    string EntryId,
    DateTimeOffset Timestamp,
    string EventType,
    string PayloadHash,
    string PreviousHash,
    string? BlockId);
