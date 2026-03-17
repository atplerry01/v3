namespace Whycespace.Engines.T3I.Reporting.Economic;

using global::System.Security.Cryptography;
using global::System.Text;
using global::System.Text.Json;

public sealed class VaultEvidenceRecorder
{
    private static readonly string[] ValidEvidenceTypes =
    [
        "VaultCreated",
        "VaultTransactionExecuted",
        "VaultContributionRecorded",
        "VaultTransferExecuted",
        "VaultWithdrawalExecuted",
        "VaultProfitDistributed",
        "VaultSnapshotCreated"
    ];

    private static readonly JsonSerializerOptions CanonicalJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
        DefaultIgnoreCondition = global::System.Text.Json.Serialization.JsonIgnoreCondition.Never
    };

    public VaultEvidenceRecord RecordEvidence(RecordVaultEvidenceCommand command)
    {
        var validationError = Validate(command);
        if (validationError is not null)
        {
            return new VaultEvidenceRecord(
                EvidenceId: command.EvidenceId,
                VaultId: command.VaultId,
                TransactionId: command.TransactionId,
                EvidenceType: command.EvidenceType,
                EvidencePayload: string.Empty,
                EvidenceTimestamp: command.EvidenceTimestamp,
                EvidenceHashCandidate: string.Empty,
                RecordedAt: DateTime.UtcNow,
                EvidenceSummary: validationError);
        }

        var payload = BuildEvidencePayload(command);
        var hashCandidate = ComputeHashCandidate(payload);

        return new VaultEvidenceRecord(
            EvidenceId: command.EvidenceId,
            VaultId: command.VaultId,
            TransactionId: command.TransactionId,
            EvidenceType: command.EvidenceType,
            EvidencePayload: payload,
            EvidenceTimestamp: command.EvidenceTimestamp,
            EvidenceHashCandidate: hashCandidate,
            RecordedAt: DateTime.UtcNow,
            EvidenceSummary: $"Evidence recorded for {command.EvidenceType} on vault {command.VaultId}");
    }

    private static string BuildEvidencePayload(RecordVaultEvidenceCommand command)
    {
        var canonical = new SortedDictionary<string, object>(StringComparer.Ordinal)
        {
            ["evidenceId"] = command.EvidenceId.ToString(),
            ["evidenceTimestamp"] = command.EvidenceTimestamp.ToString("O"),
            ["evidenceType"] = command.EvidenceType,
            ["requestedBy"] = command.RequestedBy.ToString(),
            ["transactionId"] = command.TransactionId.ToString(),
            ["vaultId"] = command.VaultId.ToString()
        };

        if (!string.IsNullOrEmpty(command.ReferenceId))
            canonical["referenceId"] = command.ReferenceId;

        if (!string.IsNullOrEmpty(command.ReferenceType))
            canonical["referenceType"] = command.ReferenceType;

        return JsonSerializer.Serialize(canonical, CanonicalJsonOptions);
    }

    private static string ComputeHashCandidate(string payload)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(payload));
        return Convert.ToHexStringLower(bytes);
    }

    private static string? Validate(RecordVaultEvidenceCommand command)
    {
        if (command.EvidenceId == Guid.Empty)
            return "EvidenceId must not be empty";

        if (command.VaultId == Guid.Empty)
            return "VaultId must not be empty";

        if (command.TransactionId == Guid.Empty)
            return "TransactionId must not be empty";

        if (command.RequestedBy == Guid.Empty)
            return "RequestedBy must not be empty";

        if (string.IsNullOrWhiteSpace(command.EvidenceType))
            return "EvidenceType must not be empty";

        if (!ValidEvidenceTypes.Contains(command.EvidenceType))
            return $"Invalid evidence type: {command.EvidenceType}";

        return null;
    }
}
