namespace Whycespace.Engines.T0U.WhyceChain.Ledger.Immutable;

using Whycespace.Systems.Upstream.WhyceChain.Models;
using Whycespace.Systems.Upstream.WhyceChain.Stores;

public sealed class ImmutableEventLedgerEngine
{
    private readonly ChainEventStore _store;

    public ImmutableEventLedgerEngine(ChainEventStore store)
    {
        _store = store;
    }

    public ChainEvent RecordEvent(string eventId, string domain, string eventType, string payloadHash)
    {
        var chainEvent = new ChainEvent(
            eventId,
            domain,
            eventType,
            payloadHash,
            DateTimeOffset.UtcNow);

        _store.AddEvent(chainEvent);
        return chainEvent;
    }

    public ChainEvent GetEvent(string eventId)
    {
        return _store.GetEvent(eventId);
    }

    public IReadOnlyCollection<ChainEvent> ListDomainEvents(string domain)
    {
        return _store.GetEventsByDomain(domain);
    }
}
