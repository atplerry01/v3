namespace Whycespace.Engines.T2E.Economic.Vault;

using Whycespace.Contracts.Engines;
using Whycespace.Runtime.EngineManifest.Attributes;
using Whycespace.Runtime.EngineManifest.Models;

[EngineManifest("VaultTransactionValidation", EngineTier.T2E, EngineKind.Validation, "ValidateVaultTransactionCommand", typeof(EngineEvent))]
public sealed class VaultTransactionValidationEngine : IEngine
{
    public string Name => "VaultTransactionValidation";

    private static readonly string[] SupportedTransactionTypes =
    {
        "Contribution", "Transfer", "Withdrawal", "Distribution", "Adjustment", "Refund"
    };

    public Task<EngineResult> ExecuteAsync(EngineContext context)
    {
        var transactionIdStr = context.Data.GetValueOrDefault("transactionId") as string;
        if (string.IsNullOrEmpty(transactionIdStr) || !Guid.TryParse(transactionIdStr, out var transactionId) || transactionId == Guid.Empty)
            return Task.FromResult(EngineResult.Fail("Missing or invalid transactionId"));

        var vaultIdStr = context.Data.GetValueOrDefault("vaultId") as string;
        if (string.IsNullOrEmpty(vaultIdStr) || !Guid.TryParse(vaultIdStr, out var vaultId) || vaultId == Guid.Empty)
            return Task.FromResult(EngineResult.Fail("Missing or invalid vaultId"));

        var vaultAccountIdStr = context.Data.GetValueOrDefault("vaultAccountId") as string;
        if (string.IsNullOrEmpty(vaultAccountIdStr) || !Guid.TryParse(vaultAccountIdStr, out var vaultAccountId) || vaultAccountId == Guid.Empty)
            return Task.FromResult(EngineResult.Fail("Missing or invalid vaultAccountId"));

        var initiatorIdStr = context.Data.GetValueOrDefault("initiatorIdentityId") as string;
        if (string.IsNullOrEmpty(initiatorIdStr) || !Guid.TryParse(initiatorIdStr, out var initiatorId) || initiatorId == Guid.Empty)
            return Task.FromResult(EngineResult.Fail("Missing or invalid initiatorIdentityId"));

        var currency = context.Data.GetValueOrDefault("currency") as string;
        if (string.IsNullOrWhiteSpace(currency))
            return Task.FromResult(EngineResult.Fail("Missing currency"));

        var amount = ResolveDecimal(context.Data.GetValueOrDefault("amount"));
        if (amount is null)
            return Task.FromResult(EngineResult.Fail("Missing or invalid amount"));
        if (amount.Value <= 0)
            return Task.FromResult(EngineResult.Fail("Amount must be greater than zero"));

        var transactionType = context.Data.GetValueOrDefault("transactionType") as string;
        if (string.IsNullOrWhiteSpace(transactionType))
            return Task.FromResult(EngineResult.Fail("Missing transactionType"));
        if (!Array.Exists(SupportedTransactionTypes, t => t == transactionType))
            return Task.FromResult(EngineResult.Fail(
                $"Unsupported transactionType: {transactionType}. Supported: {string.Join(", ", SupportedTransactionTypes)}"));

        var evaluatedAt = DateTime.UtcNow;

        // Emit validation events
        var requestedPayload = new Dictionary<string, object>
        {
            ["transactionId"] = transactionId.ToString(),
            ["vaultId"] = vaultId.ToString(),
            ["vaultAccountId"] = vaultAccountId.ToString(),
            ["initiatorIdentityId"] = initiatorId.ToString(),
            ["transactionType"] = transactionType,
            ["amount"] = amount.Value,
            ["currency"] = currency,
            ["topic"] = "whyce.economic.events"
        };

        var passedPayload = new Dictionary<string, object>
        {
            ["transactionId"] = transactionId.ToString(),
            ["vaultId"] = vaultId.ToString(),
            ["validationStatus"] = "Passed",
            ["validationReason"] = "All validation rules passed",
            ["evaluatedAt"] = evaluatedAt.ToString("O"),
            ["topic"] = "whyce.economic.events"
        };

        var events = new[]
        {
            EngineEvent.Create("VaultTransactionValidationRequested", vaultId, requestedPayload),
            EngineEvent.Create("VaultTransactionValidationPassed", vaultId, passedPayload)
        };

        var output = new Dictionary<string, object>
        {
            ["transactionId"] = transactionId.ToString(),
            ["vaultId"] = vaultId.ToString(),
            ["isValid"] = true,
            ["validationStatus"] = "Passed",
            ["validationReason"] = "All validation rules passed",
            ["evaluatedAt"] = evaluatedAt.ToString("O")
        };

        return Task.FromResult(EngineResult.Ok(events, output));
    }

    private static decimal? ResolveDecimal(object? value)
    {
        return value switch
        {
            decimal d => d,
            double d => (decimal)d,
            int i => i,
            long l => l,
            string s when decimal.TryParse(s, out var parsed) => parsed,
            _ => null
        };
    }
}
