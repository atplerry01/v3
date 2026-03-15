namespace Whycespace.System.Upstream.Governance.Models;

public sealed record GovernanceDispute(
    string DisputeId,
    string ProposalId,
    string FiledBy,
    string Reason,
    DisputeType DisputeType,
    DisputeStatus Status,
    int EscalationLevel,
    DateTime FiledAt,
    DateTime? ResolvedAt);

public enum DisputeType
{
    DecisionChallenge = 0,
    VotingIntegrity = 1,
    QuorumEvaluation = 2,
    ProposalLegitimacy = 3,
    ExecutionIntegrity = 4
}

public enum DisputeStatus
{
    Raised = 0,
    UnderReview = 1,
    Resolved = 2,
    Rejected = 3,
    Escalated = 4,
    Withdrawn = 5
}
