namespace Whycespace.System.Upstream.WhycePolicy.Models;

public sealed record PolicyAuditReport(
    IReadOnlyList<PolicyEvidenceRecord> EvidenceRecords,
    int TotalRecords,
    DateTime GeneratedAt
);
