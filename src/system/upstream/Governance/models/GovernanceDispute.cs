namespace Whycespace.System.Upstream.Governance.Models;

public sealed record GovernanceDispute(
    string DisputeId,
    string ProposalId,
    string FiledBy,
    string Reason,
    DisputeStatus Status,
    int EscalationLevel,
    DateTime FiledAt,
    DateTime? ResolvedAt);

public enum DisputeStatus
{
    Open = 0,
    Escalated = 1,
    Resolved = 2
}
