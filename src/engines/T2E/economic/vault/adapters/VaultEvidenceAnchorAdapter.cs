namespace Whycespace.Engines.T2E.Economic.Vault.Adapters;

using Whycespace.Engines.T0U.WhyceChain.Block.Builder;
using Whycespace.Engines.T0U.WhyceChain.Block.Anchor;
using Whycespace.Engines.T0U.WhyceChain.Ledger.Event;
using Whycespace.Engines.T0U.WhyceChain.Ledger.Immutable;
using Whycespace.Engines.T0U.WhyceChain.Ledger.Indexing;
using Whycespace.Engines.T0U.WhyceChain.Verification.Integrity;
using Whycespace.Engines.T0U.WhyceChain.Verification.Merkle;
using Whycespace.Engines.T0U.WhyceChain.Verification.Audit;
using Whycespace.Engines.T0U.WhyceChain.Replication.Replication;
using Whycespace.Engines.T0U.WhyceChain.Replication.Snapshot;
using Whycespace.Engines.T0U.WhyceChain.Append.Execution;
using Whycespace.Engines.T0U.WhyceChain.Evidence.Hashing;
using Whycespace.Engines.T0U.WhyceChain.Evidence.Anchoring;
using Whycespace.Engines.T0U.WhyceChain.Evidence.Gateway;

public sealed class VaultEvidenceAnchorAdapter
{
    private readonly ChainEvidenceGateway _chainGateway;

    public VaultEvidenceAnchorAdapter(ChainEvidenceGateway chainGateway)
    {
        _chainGateway = chainGateway;
    }

    public VaultEvidenceAnchorResult AnchorEvidence(AnchorVaultEvidenceCommand command)
    {
        if (command.AnchorRequestId == Guid.Empty)
            throw new ArgumentException("AnchorRequestId is required", nameof(command));

        if (command.EvidenceId == Guid.Empty)
            throw new ArgumentException("EvidenceId is required", nameof(command));

        if (command.VaultId == Guid.Empty)
            throw new ArgumentException("VaultId is required", nameof(command));

        if (string.IsNullOrWhiteSpace(command.EvidenceHash))
            throw new ArgumentException("EvidenceHash is required", nameof(command));

        if (command.RequestedBy == Guid.Empty)
            throw new ArgumentException("RequestedBy is required", nameof(command));

        var anchorPayload = new Dictionary<string, object>
        {
            ["vaultId"] = command.VaultId.ToString(),
            ["evidenceId"] = command.EvidenceId.ToString(),
            ["evidenceHash"] = command.EvidenceHash,
            ["timestamp"] = command.EvidenceTimestamp.ToString("O")
        };

        var ledgerEntry = _chainGateway.SubmitEvidence(
            command.EvidenceId.ToString(),
            "Economic",
            "VaultEvidenceAnchored",
            anchorPayload);

        return new VaultEvidenceAnchorResult(
            AnchorRequestId: command.AnchorRequestId,
            EvidenceId: command.EvidenceId,
            VaultId: command.VaultId,
            EvidenceHash: command.EvidenceHash,
            ChainTransactionId: ledgerEntry.EntryId,
            AnchorStatus: "Anchored",
            AnchoredAt: DateTime.UtcNow);
    }
}
