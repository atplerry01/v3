namespace Whycespace.Engines.T3I.Reporting.Governance.Commands;

public sealed record GenerateGovernanceAuditCommand(
    Guid CommandId,
    Guid ProposalId,
    GovernanceAuditActionType ActionType,
    Guid PerformedBy,
    Guid ActionReferenceId,
    string ActionDescription,
    DateTime Timestamp);

public enum GovernanceAuditActionType
{
    ProposalCreated,
    ProposalSubmitted,
    VoteCast,
    VotingClosed,
    QuorumEvaluated,
    DecisionApproved,
    DecisionRejected,
    DisputeRaised,
    DisputeResolved,
    EmergencyTriggered,
    EmergencyRevoked,
    WorkflowTransition,
    EvidenceRecorded
}
