using System.Text.Json;
using Whycespace.ProjectionRuntime.Projections.Core.Economics.Models;

namespace Whycespace.ProjectionRuntime.Projections.Core.Economics;

public sealed class VaultBalanceProjectionHandler
{
    public VaultBalanceModel HandleVaultCreated(
        VaultBalanceModel? existing,
        Dictionary<string, object> payload,
        DateTime timestamp)
    {
        var vaultId = payload.GetValueOrDefault("vaultId")?.ToString() ?? "";
        var balance = ExtractDecimal(payload, "balance");
        var status = payload.GetValueOrDefault("status")?.ToString() ?? "Active";

        var model = existing ?? VaultBalanceModel.Initial(vaultId, status);

        if (balance > 0m)
            model = model.ApplyCredit(balance, timestamp);

        return model.WithStatus(status, timestamp);
    }

    public VaultBalanceModel HandleContribution(
        VaultBalanceModel existing,
        Dictionary<string, object> payload,
        DateTime timestamp)
    {
        var amount = ExtractDecimal(payload, "amount");
        return existing.ApplyCredit(amount, timestamp);
    }

    public VaultBalanceModel HandleWithdrawal(
        VaultBalanceModel existing,
        Dictionary<string, object> payload,
        DateTime timestamp)
    {
        var amount = ExtractDecimal(payload, "amount");
        return existing.ApplyDebit(amount, timestamp);
    }

    public VaultBalanceModel HandleTransfer(
        VaultBalanceModel existing,
        Dictionary<string, object> payload,
        DateTime timestamp,
        string vaultId)
    {
        var sourceVaultId = payload.GetValueOrDefault("sourceVaultId")?.ToString();
        var amount = ExtractDecimal(payload, "amount");

        if (string.Equals(sourceVaultId, vaultId, StringComparison.OrdinalIgnoreCase))
            return existing.ApplyDebit(amount, timestamp);

        return existing.ApplyCredit(amount, timestamp);
    }

    public VaultBalanceModel HandleProfitDistributed(
        VaultBalanceModel existing,
        Dictionary<string, object> payload,
        DateTime timestamp)
    {
        var amount = ExtractDecimal(payload, "totalDistributed");
        if (amount == 0m)
            amount = ExtractDecimal(payload, "amount");

        return existing.ApplyDebit(amount, timestamp);
    }

    public VaultBalanceModel HandleTransactionRecorded(
        VaultBalanceModel existing,
        Dictionary<string, object> payload,
        DateTime timestamp)
    {
        var direction = payload.GetValueOrDefault("ledgerDirection")?.ToString()
                     ?? payload.GetValueOrDefault("direction")?.ToString();
        var amount = ExtractDecimal(payload, "amount");

        return direction?.Equals("Credit", StringComparison.OrdinalIgnoreCase) == true
            ? existing.ApplyCredit(amount, timestamp)
            : existing.ApplyDebit(amount, timestamp);
    }

    private static decimal ExtractDecimal(Dictionary<string, object> payload, string key)
    {
        var value = payload.GetValueOrDefault(key);
        if (value is null) return 0m;
        if (value is JsonElement element) return element.GetDecimal();
        return Convert.ToDecimal(value);
    }
}
