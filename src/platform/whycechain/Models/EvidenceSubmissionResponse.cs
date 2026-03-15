namespace Whycespace.Platform.WhyceChain.Models;

public sealed record EvidenceSubmissionResponse(
    string EvidenceHash,
    string BlockReference,
    bool SubmissionAccepted,
    DateTime GeneratedAt,
    string TraceId);