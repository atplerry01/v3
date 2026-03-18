using Whycespace.Engines.T3I.Reporting.Policy.Models;
using Whycespace.Engines.T3I.Shared;
namespace Whycespace.Engines.T3I.Reporting.Policy.Engines;

public sealed class PolicyEvidenceRecorder : IIntelligenceEngine<PolicyEvidenceInput, PolicyEvidenceRecord>
{
    public string EngineName => "PolicyEvidence";

    public IntelligenceResult<PolicyEvidenceRecord> Execute(IntelligenceContext<PolicyEvidenceInput> context)
    {
        var startedAt = DateTimeOffset.UtcNow;
        var result = RecordEvidence(context.Input);
        return IntelligenceResult<PolicyEvidenceRecord>.Ok(result, IntelligenceTrace.Create(EngineName, context.CorrelationId, startedAt));
    }

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
