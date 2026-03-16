namespace Whycespace.Engines.T0U.WhyceChain;

using Whycespace.Systems.Upstream.WhyceChain.Models;
using Whycespace.Systems.Upstream.WhyceChain.Stores;

public sealed partial class EvidenceAnchoringEngine
{
    private readonly ChainLedgerEngine _ledgerEngine;
    private readonly EvidenceHashEngine _hashEngine;
    private readonly ImmutableEventLedgerEngine _eventLedgerEngine;

    public EvidenceAnchoringEngine(
        ChainLedgerEngine ledgerEngine,
        EvidenceHashEngine hashEngine,
        ImmutableEventLedgerEngine eventLedgerEngine)
    {
        _ledgerEngine = ledgerEngine;
        _hashEngine = hashEngine;
        _eventLedgerEngine = eventLedgerEngine;
    }

    public Whycespace.Systems.Upstream.WhyceChain.Models.ChainLedgerEntry AnchorEvidence(string evidenceId, string domain, string eventType, object payload)
    {
        var hash = _hashEngine.HashObject(payload);
        _eventLedgerEngine.RecordEvent(evidenceId, domain, eventType, hash.Hash);
        return _ledgerEngine.RegisterEntry(evidenceId, eventType, hash.Hash);
    }

    public EvidenceHash GetEvidenceProof(string evidenceId)
    {
        var entry = _ledgerEngine.GetEntry(evidenceId);
        return new EvidenceHash(entry.PayloadHash, "SHA256", entry.Timestamp);
    }
}
