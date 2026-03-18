namespace Whycespace.Platform.WhyceChain;

using Whycespace.Engines.T0U.WhyceChain.Evidence.Anchoring;
using Whycespace.Engines.T0U.WhyceChain.Evidence.Hashing;
using Whycespace.Engines.T0U.WhyceChain.Verification.Integrity;
using Whycespace.Engines.T0U.WhyceChain.Verification.Merkle;
using Whycespace.Platform.WhyceChain.Models;
using Whycespace.Systems.Upstream.WhyceChain.Models;
using Whycespace.Systems.Upstream.WhyceChain.Stores;

public sealed class ChainEvidenceGateway
{
    private readonly EvidenceAnchoringEngine _anchoringEngine;
    private readonly EvidenceHashEngine _hashEngine;
    private readonly IntegrityVerificationEngine _integrityEngine;
    private readonly MerkleProofEngine _merkleEngine;
    private readonly ChainBlockStore _blockStore;

    public ChainEvidenceGateway(
        EvidenceAnchoringEngine anchoringEngine,
        EvidenceHashEngine hashEngine,
        IntegrityVerificationEngine integrityEngine,
        ChainBlockStore blockStore)
    {
        _anchoringEngine = anchoringEngine;
        _hashEngine = hashEngine;
        _integrityEngine = integrityEngine;
        _merkleEngine = new MerkleProofEngine();
        _blockStore = blockStore;
    }

    public EvidenceSubmissionResponse SubmitEvidence(EvidenceSubmissionRequest request)
    {
        ValidateSubmissionRequest(request);

        var entry = _anchoringEngine.AnchorEvidence(
            Guid.NewGuid().ToString(),
            request.OriginSystem,
            request.EvidenceType,
            request.EvidencePayload);

        return new EvidenceSubmissionResponse(
            EvidenceHash: entry.PayloadHash,
            BlockReference: entry.BlockId ?? "pending",
            SubmissionAccepted: true,
            GeneratedAt: DateTime.UtcNow,
            TraceId: request.TraceId);
    }

    public EvidenceVerificationResponse VerifyEvidence(EvidenceVerificationRequest request)
    {
        ValidateVerificationRequest(request);

        var evidenceExists = false;
        var merkleProofValid = false;
        var blockIntegrityValid = false;

        try
        {
            var latestBlock = _blockStore.GetLatestBlock();
            if (latestBlock is not null)
            {
                ChainBlock? targetBlock = null;

                if (!string.IsNullOrEmpty(request.BlockHash))
                {
                    for (long i = 0; i <= latestBlock.BlockNumber; i++)
                    {
                        var block = _blockStore.GetBlock(i);
                        if (block.BlockHash == request.BlockHash)
                        {
                            targetBlock = block;
                            break;
                        }
                    }
                }
                else
                {
                    targetBlock = latestBlock;
                }

                if (targetBlock is not null)
                {
                    var command = new IntegrityVerificationCommand(
                        Array.Empty<ChainLedgerEntry>(),
                        [targetBlock],
                        MerkleProof: null,
                        TraceId: request.TraceId,
                        CorrelationId: request.CorrelationId,
                        Timestamp: DateTimeOffset.UtcNow);

                    var result = _integrityEngine.Execute(command);
                    blockIntegrityValid = result.BlockChainValid && result.MerkleRootValid;
                    merkleProofValid = blockIntegrityValid;

                    evidenceExists = targetBlock.EntryIds.Any(id =>
                    {
                        try
                        {
                            var proof = _anchoringEngine.GetEvidenceProof(id);
                            return proof.Hash == request.EvidenceHash;
                        }
                        catch (KeyNotFoundException)
                        {
                            return false;
                        }
                    });
                }
            }
        }
        catch (KeyNotFoundException)
        {
            // Evidence or block not found — return defaults
        }

        return new EvidenceVerificationResponse(
            EvidenceExists: evidenceExists,
            MerkleProofValid: merkleProofValid,
            BlockIntegrityValid: blockIntegrityValid,
            VerificationTimestamp: DateTime.UtcNow,
            TraceId: request.TraceId);
    }

    private static void ValidateSubmissionRequest(EvidenceSubmissionRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.EvidenceType))
            throw new ArgumentException("EvidenceType is required.", nameof(request));
        if (request.EvidencePayload is null)
            throw new ArgumentException("EvidencePayload is required.", nameof(request));
        if (string.IsNullOrWhiteSpace(request.OriginSystem))
            throw new ArgumentException("OriginSystem is required.", nameof(request));
        if (string.IsNullOrWhiteSpace(request.TraceId))
            throw new ArgumentException("TraceId is required.", nameof(request));
        if (string.IsNullOrWhiteSpace(request.CorrelationId))
            throw new ArgumentException("CorrelationId is required.", nameof(request));
    }

    private static void ValidateVerificationRequest(EvidenceVerificationRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.EvidenceHash))
            throw new ArgumentException("EvidenceHash is required.", nameof(request));
        if (string.IsNullOrWhiteSpace(request.TraceId))
            throw new ArgumentException("TraceId is required.", nameof(request));
        if (string.IsNullOrWhiteSpace(request.CorrelationId))
            throw new ArgumentException("CorrelationId is required.", nameof(request));
    }
}
