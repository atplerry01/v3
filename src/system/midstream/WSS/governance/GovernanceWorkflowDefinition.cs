namespace Whycespace.System.Midstream.WSS.Governance;

public sealed record GovernanceWorkflowDefinition(
    string WorkflowId,
    string Name,
    IReadOnlyList<GovernanceWorkflowStep> Steps,
    DateTimeOffset CreatedAt)
{
    public static GovernanceWorkflowDefinition Default() =>
        new(
            "governance-proposal-lifecycle",
            "Governance Proposal Lifecycle",
            new[]
            {
                GovernanceWorkflowStep.ProposalCreated,
                GovernanceWorkflowStep.ProposalSubmitted,
                GovernanceWorkflowStep.ProposalUnderReview,
                GovernanceWorkflowStep.VotingOpen,
                GovernanceWorkflowStep.VotingClosed,
                GovernanceWorkflowStep.QuorumEvaluation,
                GovernanceWorkflowStep.GovernanceDecision,
                GovernanceWorkflowStep.GovernanceExecution,
                GovernanceWorkflowStep.WorkflowCompleted
            },
            DateTimeOffset.UtcNow);
}
