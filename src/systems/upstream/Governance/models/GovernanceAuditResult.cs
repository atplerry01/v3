namespace Whycespace.Systems.Upstream.Governance.Models;

public sealed record GovernanceAuditResult(
    string AuditId,
    string TargetId,
    AuditTargetType TargetType,
    bool HasEvidence,
    bool IsValid,
    IReadOnlyList<string> Findings,
    DateTime AuditedAt);

public enum AuditTargetType
{
    Proposal,
    Vote,
    Decision
}
