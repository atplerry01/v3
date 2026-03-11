namespace Whycespace.Engines.T0U.WhyceChain;

using Whycespace.System.Upstream.WhyceChain.Models;
using Whycespace.System.Upstream.WhyceChain.Stores;

public sealed class ChainLedgerEngine
{
    private readonly ChainLedgerStore _store;
    private readonly object _lock = new();
    private string _lastHash = "genesis";

    public ChainLedgerEngine(ChainLedgerStore store)
    {
        _store = store;
    }

    public ChainLedgerEntry RegisterEntry(string entryId, string eventType, string payloadHash)
    {
        lock (_lock)
        {
            var entry = new ChainLedgerEntry(
                entryId,
                DateTimeOffset.UtcNow,
                eventType,
                payloadHash,
                _lastHash,
                BlockId: null);

            _store.AddEntry(entry);
            _lastHash = payloadHash;

            return entry;
        }
    }

    public ChainLedgerEntry GetEntry(string entryId)
    {
        return _store.GetEntry(entryId);
    }

    public IReadOnlyCollection<ChainLedgerEntry> ListEntries()
    {
        return _store.GetAllEntries();
    }
}
