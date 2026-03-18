namespace Whycespace.Engines.T0U.WhyceChain.Evidence.Gateway;

using Whycespace.Systems.Upstream.WhyceChain.Models;

public sealed class ChainEvidenceGateway
{
    private readonly EvidenceAnchoringEngine _anchoringEngine;
    private readonly EvidenceHashEngine _hashEngine;

    public ChainEvidenceGateway(
        EvidenceAnchoringEngine anchoringEngine,
        EvidenceHashEngine hashEngine)
    {
        _anchoringEngine = anchoringEngine;
        _hashEngine = hashEngine;
    }

    public ChainLedgerEntry SubmitEvidence(string evidenceId, string domain, string eventType, object payload)
    {
        return _anchoringEngine.AnchorEvidence(evidenceId, domain, eventType, payload);
    }

    public EvidenceHash GetEvidence(string evidenceId)
    {
        return _anchoringEngine.GetEvidenceProof(evidenceId);
    }

    public bool VerifyEvidence(string evidenceId, object payload)
    {
        var proof = _anchoringEngine.GetEvidenceProof(evidenceId);
        var hash = _hashEngine.HashObject(payload);
        return proof.Hash == hash.Hash;
    }
}
