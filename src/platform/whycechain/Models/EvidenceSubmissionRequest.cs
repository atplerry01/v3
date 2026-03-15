namespace Whycespace.Platform.WhyceChain.Models;

public sealed record EvidenceSubmissionRequest(
    string EvidenceType,
    object EvidencePayload,
    string OriginSystem,
    string TraceId,
    string CorrelationId,
    DateTime Timestamp);