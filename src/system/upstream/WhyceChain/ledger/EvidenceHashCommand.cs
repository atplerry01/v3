namespace Whycespace.System.Upstream.WhyceChain.Ledger;

public sealed record EvidenceHashCommand(
    string EvidenceType,
    object EvidencePayload,
    string TraceId,
    string CorrelationId,
    DateTime Timestamp);
