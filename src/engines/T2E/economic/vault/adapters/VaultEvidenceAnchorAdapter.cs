namespace Whycespace.Engines.T2E.Economic.Vault.Adapters;

using Whycespace.Engines.T0U.WhyceChain;

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
