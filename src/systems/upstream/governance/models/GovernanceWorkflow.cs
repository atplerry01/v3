namespace Whycespace.Systems.Upstream.Governance.Models;

public sealed record GovernanceWorkflow(
    string WorkflowId,
    string ProposalId,
    WorkflowStage Stage,
    DateTime StartedAt,
    DateTime? CompletedAt);

public enum WorkflowStage
{
    Create = 0,
    Review = 1,
    Voting = 2,
    Decision = 3,
    Execution = 4,
    Completed = 5
}
