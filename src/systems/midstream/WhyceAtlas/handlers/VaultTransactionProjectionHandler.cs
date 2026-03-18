using System.Text.Json;

namespace Whycespace.Systems.Midstream.WhyceAtlas.Projections.Vault;

public static class VaultTransactionProjectionHandler
{
    public static VaultTransactionReadModel? Handle(string eventType, Dictionary<string, object> payload)
    {
        var transactionId = ExtractGuid(payload, "transactionId");
        var vaultId = ExtractGuid(payload, "vaultId");
        var participantId = ExtractGuid(payload, "participantId");
        var amount = ExtractDecimal(payload, "amount");
        var currency = payload.GetValueOrDefault("currency")?.ToString() ?? "WCE";
        var reference = payload.GetValueOrDefault("transactionReference")?.ToString();
        var timestamp = ExtractDateTime(payload, "timestamp");

        var (transactionType, status, summary) = eventType switch
        {
            "VaultContributionRecorded" => ("Contribution", "Completed", "Vault contribution recorded"),
            "VaultWithdrawalExecuted" => ("Withdrawal", "Completed", "Vault withdrawal executed"),
            "VaultTransferExecuted" => ("Transfer", "Completed", "Vault transfer executed"),
            "VaultProfitDistributed" => ("ProfitDistribution", "Completed", "Vault profit distribution"),
            "VaultTransactionCreated" => (
                payload.GetValueOrDefault("transactionType")?.ToString() ?? "Unknown",
                "Completed",
                "Vault transaction created"),
            "VaultTransactionCompleted" => (
                payload.GetValueOrDefault("transactionType")?.ToString() ?? "Unknown",
                "Completed",
                "Vault transaction completed"),
            _ => (null, null, null)
        };

        if (transactionType is null)
            return null;

        return new VaultTransactionReadModel(
            transactionId,
            vaultId,
            participantId,
            transactionType,
            amount,
            currency,
            status!,
            timestamp,
            RecordedAt: DateTime.UtcNow,
            TransactionReference: reference,
            TransactionSummary: summary);
    }

    private static Guid ExtractGuid(Dictionary<string, object> payload, string key)
    {
        var raw = payload.GetValueOrDefault(key)?.ToString();
        return Guid.TryParse(raw, out var id) ? id : Guid.Empty;
    }

    private static decimal ExtractDecimal(Dictionary<string, object> payload, string key)
    {
        var raw = payload.GetValueOrDefault(key);
        if (raw is JsonElement je)
            return je.TryGetDecimal(out var d) ? d : 0m;
        return Convert.ToDecimal(raw ?? 0);
    }

    private static DateTime ExtractDateTime(Dictionary<string, object> payload, string key)
    {
        var raw = payload.GetValueOrDefault(key)?.ToString();
        return DateTime.TryParse(raw, out var dt) ? dt : DateTime.UtcNow;
    }
}
