namespace Whycespace.Engines.T0U.WhyceChain.Evidence.Anchoring;

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Whycespace.Systems.Upstream.WhyceChain.Ledger;
using Whycespace.Systems.Upstream.WhyceChain.Models;
using Whycespace.Systems.Upstream.WhyceChain.Stores;

public sealed class EvidenceAnchoringEngine
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

    public EvidenceAnchorResult Execute(EvidenceAnchorCommand command)
    {
        if (string.IsNullOrWhiteSpace(command.Block.MerkleRoot))
            throw new ArgumentException("MerkleRoot is required.");
        if (string.IsNullOrWhiteSpace(command.Block.BlockHash))
            throw new ArgumentException("BlockHash is required.");
        if (string.IsNullOrWhiteSpace(command.AnchorTarget))
            throw new ArgumentException("AnchorTarget is required.");

        var payload = JsonSerializer.Serialize(new
        {
            blockHeight = command.Block.BlockHeight,
            blockHash = command.Block.BlockHash,
            merkleRoot = command.Block.MerkleRoot,
            entryCount = command.Block.EntryCount,
            createdAt = command.Block.CreatedAt
        });

        var payloadHash = ComputeSha256(payload);
        var referenceId = ComputeSha256($"{command.Block.BlockHash}:{command.AnchorTarget}:{command.Block.MerkleRoot}");

        return new EvidenceAnchorResult(
            command.Block.BlockHash,
            command.Block.MerkleRoot,
            payload,
            payloadHash,
            command.AnchorTarget,
            referenceId,
            DateTime.UtcNow,
            command.TraceId);
    }

    private static string ComputeSha256(string input)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexStringLower(bytes);
    }
}
