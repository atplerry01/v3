namespace Whycespace.Systems.Midstream.Capital.Evidence;

public interface ICapitalEvidenceRecorder
{
    Task<CapitalEvidenceRecord> RecordEvidenceAsync(CapitalEvidenceRecord record);

    Task<IReadOnlyList<CapitalEvidenceRecord>> GetEvidenceByCapitalIdAsync(Guid capitalId);

    Task<IReadOnlyList<CapitalEvidenceRecord>> GetEvidenceByReferenceIdAsync(Guid referenceId);

    Task<bool> VerifyEvidenceIntegrityAsync(Guid evidenceId);
}
