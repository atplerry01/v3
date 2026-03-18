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

namespace Whycespace.WhyceChain.Tests;

public class EvidenceHashEngineTests
{
    private readonly EvidenceHashEngine _engine;

    public EvidenceHashEngineTests()
    {
        _engine = new EvidenceHashEngine();
    }

    [Fact]
    public void HashPayload_ShouldReturnDeterministicHash()
    {
        var first = _engine.HashPayload("test-payload");
        var second = _engine.HashPayload("test-payload");

        Assert.Equal(first.Hash, second.Hash);
        Assert.Equal("SHA256", first.Algorithm);
    }

    [Fact]
    public void HashObject_ShouldHashJsonRepresentation()
    {
        var obj = new { Name = "policy-1", Value = 42 };

        var first = _engine.HashObject(obj);
        var second = _engine.HashObject(obj);

        Assert.Equal(first.Hash, second.Hash);
        Assert.Equal("SHA256", first.Algorithm);
    }

    [Fact]
    public void VerifyHash_ShouldConfirmIntegrity()
    {
        var payload = "evidence-data";
        var result = _engine.HashPayload(payload);

        Assert.True(_engine.VerifyHash(payload, result.Hash));
        Assert.False(_engine.VerifyHash("tampered-data", result.Hash));
    }
}
