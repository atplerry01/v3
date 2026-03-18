namespace Whycespace.Systems.Upstream.Governance.Evidence.Models;

public sealed record GovernanceEvidenceRecord(
    Guid EvidenceId,
    Guid ProposalId,
    EvidenceType EvidenceType,
    Guid EventReferenceId,
    string EvidenceHash,
    string MerkleRoot,
    Guid RecordedBy,
    DateTime RecordedAt);

public enum EvidenceType
{
    ProposalCreated,
    VoteCast,
    QuorumEvaluated,
    DecisionMade,
    DisputeRaised,
    EmergencyAction,
    WorkflowTransition
}
