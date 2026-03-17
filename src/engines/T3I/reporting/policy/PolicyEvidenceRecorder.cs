namespace Whycespace.Engines.T3I.Reporting.Policy;

public sealed class PolicyEvidenceRecorder
{
    public PolicyEvidenceRecord RecordEvidence(PolicyEvidenceInput input)
    {
        var contextHash = PolicyEvidenceHashGenerator.GenerateContextHash(input.EvidenceContext);
        var evidenceHash = PolicyEvidenceHashGenerator.GenerateEvidenceHash(
            input.PolicyId, input.ActionType, input.ActorId, contextHash, input.Timestamp);
        var evidenceId = PolicyEvidenceHashGenerator.GenerateEvidenceId(
            input.PolicyId, input.ActionType, input.ActorId, evidenceHash, input.Timestamp);

        return new PolicyEvidenceRecord(
            evidenceId,
            input.PolicyId,
            input.ActionType,
            input.ActorId,
            evidenceHash,
            contextHash,
            input.Timestamp
        );
    }
}
