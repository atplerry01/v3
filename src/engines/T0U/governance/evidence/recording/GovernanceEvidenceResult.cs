namespace Whycespace.Engines.T0U.Governance.Evidence.Recording;

using Whycespace.Systems.Upstream.Governance.Evidence.Models;

public sealed record GovernanceEvidenceResult(
    bool Success,
    Guid EvidenceId,
    Guid ProposalId,
    EvidenceType EvidenceType,
    string EvidenceHash,
    string MerkleRoot,
    string Message,
    DateTime ExecutedAt);
