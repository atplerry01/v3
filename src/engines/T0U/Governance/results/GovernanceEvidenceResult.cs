namespace Whycespace.Engines.T0U.Governance.Results;

using Whycespace.System.Upstream.Governance.Evidence.Models;

public sealed record GovernanceEvidenceResult(
    bool Success,
    Guid EvidenceId,
    Guid ProposalId,
    EvidenceType EvidenceType,
    string EvidenceHash,
    string MerkleRoot,
    string Message,
    DateTime ExecutedAt);
