using Whycespace.Engines.T0U.WhyceChain;
using Whycespace.Platform.WhyceChain;
using Whycespace.Platform.WhyceChain.Models;
using Whycespace.System.Upstream.WhyceChain.Stores;

namespace Whycespace.WhyceChainGateway.Tests;

public class ChainEvidenceGatewayTests
{
    private readonly Whycespace.Platform.WhyceChain.ChainEvidenceGateway _gateway;

    public ChainEvidenceGatewayTests()
    {
        var ledgerStore = new ChainLedgerStore();
        var blockStore = new ChainBlockStore();
        var eventStore = new ChainEventStore();
        var ledgerEngine = new ChainLedgerEngine(ledgerStore);
        var hashEngine = new EvidenceHashEngine();
        var eventLedgerEngine = new ImmutableEventLedgerEngine(eventStore);
        var anchoringEngine = new EvidenceAnchoringEngine(ledgerEngine, hashEngine, eventLedgerEngine);
        var merkleEngine = new MerkleProofEngine();
        var integrityEngine = new IntegrityVerificationEngine(merkleEngine);
        _gateway = new Whycespace.Platform.WhyceChain.ChainEvidenceGateway(anchoringEngine, hashEngine, integrityEngine, blockStore);
    }

    [Fact]
    public void SubmitEvidence_ShouldReturnAcceptedResponse()
    {
        var request = new EvidenceSubmissionRequest(
            EvidenceType: "PolicyDecision",
            EvidencePayload: new { Action = "Approved", PolicyId = "p-1" },
            OriginSystem: "WhycePolicy",
            TraceId: "trace-1",
            CorrelationId: "corr-1",
            Timestamp: DateTime.UtcNow);

        var response = _gateway.SubmitEvidence(request);

        Assert.True(response.SubmissionAccepted);
        Assert.NotEmpty(response.EvidenceHash);
        Assert.Equal("trace-1", response.TraceId);
        Assert.Equal("pending", response.BlockReference);
    }

    [Fact]
    public void SubmitEvidence_ShouldRejectMissingEvidenceType()
    {
        var request = new EvidenceSubmissionRequest(
            EvidenceType: "",
            EvidencePayload: new { Data = "test" },
            OriginSystem: "TestSystem",
            TraceId: "trace-1",
            CorrelationId: "corr-1",
            Timestamp: DateTime.UtcNow);

        Assert.Throws<ArgumentException>(() => _gateway.SubmitEvidence(request));
    }

    [Fact]
    public void SubmitEvidence_ShouldRejectMissingOriginSystem()
    {
        var request = new EvidenceSubmissionRequest(
            EvidenceType: "Vote",
            EvidencePayload: new { Vote = "Yes" },
            OriginSystem: "",
            TraceId: "trace-1",
            CorrelationId: "corr-1",
            Timestamp: DateTime.UtcNow);

        Assert.Throws<ArgumentException>(() => _gateway.SubmitEvidence(request));
    }

    [Fact]
    public void SubmitEvidence_ShouldRejectMissingTraceId()
    {
        var request = new EvidenceSubmissionRequest(
            EvidenceType: "Vote",
            EvidencePayload: new { Vote = "Yes" },
            OriginSystem: "Governance",
            TraceId: "",
            CorrelationId: "corr-1",
            Timestamp: DateTime.UtcNow);

        Assert.Throws<ArgumentException>(() => _gateway.SubmitEvidence(request));
    }

    [Fact]
    public void SubmitEvidence_ShouldRejectMissingCorrelationId()
    {
        var request = new EvidenceSubmissionRequest(
            EvidenceType: "Vote",
            EvidencePayload: new { Vote = "Yes" },
            OriginSystem: "Governance",
            TraceId: "trace-1",
            CorrelationId: "",
            Timestamp: DateTime.UtcNow);

        Assert.Throws<ArgumentException>(() => _gateway.SubmitEvidence(request));
    }

    [Fact]
    public void VerifyEvidence_ShouldReturnVerificationResponse()
    {
        var request = new EvidenceVerificationRequest(
            EvidenceHash: "some-hash",
            BlockHash: "",
            TraceId: "trace-1",
            CorrelationId: "corr-1",
            Timestamp: DateTime.UtcNow);

        var response = _gateway.VerifyEvidence(request);

        Assert.False(response.EvidenceExists);
        Assert.Equal("trace-1", response.TraceId);
    }

    [Fact]
    public void VerifyEvidence_ShouldRejectMissingEvidenceHash()
    {
        var request = new EvidenceVerificationRequest(
            EvidenceHash: "",
            BlockHash: "block-1",
            TraceId: "trace-1",
            CorrelationId: "corr-1",
            Timestamp: DateTime.UtcNow);

        Assert.Throws<ArgumentException>(() => _gateway.VerifyEvidence(request));
    }

    [Fact]
    public void VerifyEvidence_ShouldRejectMissingTraceId()
    {
        var request = new EvidenceVerificationRequest(
            EvidenceHash: "hash-1",
            BlockHash: "",
            TraceId: "",
            CorrelationId: "corr-1",
            Timestamp: DateTime.UtcNow);

        Assert.Throws<ArgumentException>(() => _gateway.VerifyEvidence(request));
    }

    [Fact]
    public void SubmitEvidence_ShouldProduceDeterministicHash()
    {
        var payload = new { Action = "Approved", PolicyId = "p-1" };

        var request1 = new EvidenceSubmissionRequest(
            EvidenceType: "PolicyDecision",
            EvidencePayload: payload,
            OriginSystem: "WhycePolicy",
            TraceId: "trace-1",
            CorrelationId: "corr-1",
            Timestamp: DateTime.UtcNow);

        var request2 = new EvidenceSubmissionRequest(
            EvidenceType: "PolicyDecision",
            EvidencePayload: payload,
            OriginSystem: "WhycePolicy",
            TraceId: "trace-2",
            CorrelationId: "corr-2",
            Timestamp: DateTime.UtcNow);

        var response1 = _gateway.SubmitEvidence(request1);
        var response2 = _gateway.SubmitEvidence(request2);

        Assert.Equal(response1.EvidenceHash, response2.EvidenceHash);
    }
}
