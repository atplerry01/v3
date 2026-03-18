namespace Whycespace.Systems.Midstream.WSS.Governance;

public enum GovernanceWorkflowStep
{
    ProposalCreated = 0,
    ProposalSubmitted = 1,
    ProposalUnderReview = 2,
    VotingOpen = 3,
    VotingClosed = 4,
    QuorumEvaluation = 5,
    GovernanceDecision = 6,
    GovernanceExecution = 7,
    WorkflowCompleted = 8
}
