namespace Whycespace.Systems.Upstream.WhycePolicy.Workflows;

public sealed record PolicyGovernanceState(
    string WorkflowId,
    string PolicyId,
    PolicyGovernanceStage CurrentStage,
    string SubmittedBy,
    DateTimeOffset SubmittedAt,
    PolicyApprovalStatus ApprovalStatus,
    DateTimeOffset? ActivatedAt
);

public enum PolicyGovernanceStage
{
    Submitted,
    Simulation,
    ConflictAnalysis,
    GovernanceReview,
    ApprovalRequired,
    Activated,
    Rejected
}

public enum PolicyApprovalStatus
{
    Pending,
    Approved,
    Rejected
}
