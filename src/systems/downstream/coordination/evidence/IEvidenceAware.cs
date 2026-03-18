namespace Whycespace.Systems.Downstream.Coordination.Evidence;

public interface IEvidenceAware
{
    EvidenceRecord RecordEvidence(string action, string outcome, string correlationId);
}
