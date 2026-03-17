namespace Whycespace.Systems.Upstream.WhycePolicy.Models;

public sealed record PolicyAuditReport(
    IReadOnlyList<PolicyEvidenceRecord> EvidenceRecords,
    int TotalRecords,
    DateTime GeneratedAt,
    string? PolicyId = null,
    IReadOnlyList<PolicyAuditEntry>? AuditEntries = null,
    int TotalEntries = 0,
    bool EvidenceLinked = false
);
