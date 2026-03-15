namespace Whycespace.Platform.WhyceChain.Models;

public sealed record EvidenceVerificationRequest(
    string EvidenceHash,
    string BlockHash,
    string TraceId,
    string CorrelationId,
    DateTime Timestamp);