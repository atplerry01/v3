namespace Whycespace.Engines.T0U.Governance.Evidence.Recording;

using Whycespace.Systems.Upstream.Governance.Evidence.Models;

public sealed record RecordGovernanceEvidenceCommand(
    Guid CommandId,
    Guid ProposalId,
    Guid EventReferenceId,
    EvidenceType EvidenceType,
    Guid RecordedByGuardianId,
    string EvidencePayload,
    DateTime Timestamp);
