using Whycespace.Engines.T0U.WhyceChain.Block.Builder;
using Whycespace.Engines.T0U.WhyceChain.Block.Anchor;
using Whycespace.Engines.T0U.WhyceChain.Ledger.Event;
using Whycespace.Engines.T0U.WhyceChain.Ledger.Immutable;
using Whycespace.Engines.T0U.WhyceChain.Ledger.Indexing;
using Whycespace.Engines.T0U.WhyceChain.Verification.Integrity;
using Whycespace.Engines.T0U.WhyceChain.Verification.Merkle;
using Whycespace.Engines.T0U.WhyceChain.Verification.Audit;
using Whycespace.Engines.T0U.WhyceChain.Replication.Replication;
using Whycespace.Engines.T0U.WhyceChain.Replication.Snapshot;
using Whycespace.Engines.T0U.WhyceChain.Append.Execution;
using Whycespace.Engines.T0U.WhyceChain.Evidence.Hashing;
using Whycespace.Engines.T0U.WhyceChain.Evidence.Anchoring;
using Whycespace.Engines.T0U.WhyceChain.Evidence.Gateway;
using Whycespace.Systems.Upstream.WhyceChain.Stores;

namespace Whycespace.WhyceChain.Tests;

public class ImmutableEventLedgerEngineTests
{
    private readonly ChainEventStore _store;
    private readonly ImmutableEventLedgerEngine _engine;

    public ImmutableEventLedgerEngineTests()
    {
        _store = new ChainEventStore();
        _engine = new ImmutableEventLedgerEngine(_store);
    }

    [Fact]
    public void RecordEvent_ShouldStoreAndReturnEvent()
    {
        var result = _engine.RecordEvent("evt-1", "Policy", "PolicyDecision", "hash-abc");

        Assert.Equal("evt-1", result.EventId);
        Assert.Equal("Policy", result.Domain);
        Assert.Equal("PolicyDecision", result.EventType);
        Assert.Equal("hash-abc", result.PayloadHash);
    }

    [Fact]
    public void RecordEvent_DuplicateId_ShouldThrow()
    {
        _engine.RecordEvent("evt-1", "Policy", "PolicyDecision", "hash-abc");

        Assert.Throws<InvalidOperationException>(() =>
            _engine.RecordEvent("evt-1", "Finance", "Transaction", "hash-def"));
    }

    [Fact]
    public void ListDomainEvents_ShouldFilterByDomain()
    {
        _engine.RecordEvent("evt-1", "Policy", "PolicyDecision", "hash-abc");
        _engine.RecordEvent("evt-2", "Finance", "Transaction", "hash-def");
        _engine.RecordEvent("evt-3", "Policy", "GovernanceVote", "hash-ghi");

        var policyEvents = _engine.ListDomainEvents("Policy");
        var financeEvents = _engine.ListDomainEvents("Finance");

        Assert.Equal(2, policyEvents.Count);
        Assert.Single(financeEvents);
    }
}
