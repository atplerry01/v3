namespace Whycespace.Domain.Governance.Events;

public sealed record GovernanceEvidenceRecordedEvent(
    Guid EventId,
    Guid EvidenceId,
    Guid ProposalId,
    string EvidenceType,
    Guid EventReferenceId,
    string EvidenceHash,
    string MerkleRoot,
    Guid RecordedByGuardianId,
    DateTime RecordedAt,
    int EventVersion)
{
    public static GovernanceEvidenceRecordedEvent Create(
        Guid evidenceId,
        Guid proposalId,
        string evidenceType,
        Guid eventReferenceId,
        string evidenceHash,
        string merkleRoot,
        Guid recordedByGuardianId)
        => new(Guid.NewGuid(), evidenceId, proposalId, evidenceType, eventReferenceId,
            evidenceHash, merkleRoot, recordedByGuardianId, DateTime.UtcNow, 1);
}
