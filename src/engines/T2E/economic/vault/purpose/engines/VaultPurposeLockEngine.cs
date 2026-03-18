namespace Whycespace.Engines.T2E.Economic.Vault.Purpose.Engines;

using Whycespace.Contracts.Engines;
using Whycespace.Runtime.EngineManifest.Attributes;
using Whycespace.Runtime.EngineManifest.Models;

[EngineManifest("VaultPurposeLock", EngineTier.T2E, EngineKind.Validation, "ValidateVaultPurposeCommand", typeof(EngineEvent))]
public sealed class VaultPurposeLockEngine : IEngine
{
    public string Name => "VaultPurposeLock";

    private static readonly string[] ValidPurposes =
    {
        "GeneralPurpose", "InvestmentCapital", "SPVCapital", "RevenueCollection",
        "ProfitDistribution", "OperationalTreasury", "Escrow", "InfrastructureFunding", "GrantFunding"
    };

    private static readonly string[] ValidTransactionTypes =
    {
        "Contribution", "Transfer", "Withdrawal", "Distribution", "ProfitDistribution",
        "Adjustment", "Refund"
    };

    private static readonly Dictionary<string, string[]> AllowedTransactions = new()
    {
        ["InvestmentCapital"] = ["Contribution", "Transfer", "ProfitDistribution"],
        ["SPVCapital"] = ["Contribution", "Transfer", "ProfitDistribution"],
        ["Escrow"] = ["Contribution"],
        ["OperationalTreasury"] = ["Contribution", "Transfer", "Withdrawal"],
        ["InfrastructureFunding"] = ["Contribution", "Transfer"],
        ["RevenueCollection"] = ["Contribution", "Transfer", "Withdrawal", "Distribution"],
        ["ProfitDistribution"] = ["Contribution", "Distribution", "ProfitDistribution"],
        ["GrantFunding"] = ["Contribution", "Transfer"],
        ["GeneralPurpose"] = ["Contribution", "Transfer", "Withdrawal", "Distribution", "ProfitDistribution", "Adjustment", "Refund"]
    };

    public Task<EngineResult> ExecuteAsync(EngineContext context)
    {
        // --- Validate VaultId ---
        var vaultIdRaw = context.Data.GetValueOrDefault("vaultId") as string;
        if (string.IsNullOrEmpty(vaultIdRaw))
            return Task.FromResult(EngineResult.Fail("Missing vaultId"));
        if (!Guid.TryParse(vaultIdRaw, out var vaultId) || vaultId == Guid.Empty)
            return Task.FromResult(EngineResult.Fail("Invalid vaultId format"));

        // --- Validate VaultPurpose ---
        var vaultPurpose = context.Data.GetValueOrDefault("vaultPurpose") as string;
        if (string.IsNullOrEmpty(vaultPurpose))
            return Task.FromResult(EngineResult.Fail("Missing vaultPurpose"));
        if (!Array.Exists(ValidPurposes, p => p == vaultPurpose))
            return Task.FromResult(EngineResult.Fail($"Invalid vaultPurpose: {vaultPurpose}. Valid: {string.Join(", ", ValidPurposes)}"));

        // --- Validate TransactionType ---
        var transactionType = context.Data.GetValueOrDefault("transactionType") as string;
        if (string.IsNullOrEmpty(transactionType))
            return Task.FromResult(EngineResult.Fail("Missing transactionType"));
        if (!Array.Exists(ValidTransactionTypes, t => t == transactionType))
            return Task.FromResult(EngineResult.Fail($"Invalid transactionType: {transactionType}. Valid: {string.Join(", ", ValidTransactionTypes)}"));

        // --- Validate Amount ---
        var amount = ResolveDecimal(context.Data.GetValueOrDefault("amount"));
        if (amount is null)
            return Task.FromResult(EngineResult.Fail("Missing or invalid amount"));
        if (amount.Value <= 0)
            return Task.FromResult(EngineResult.Fail("Amount must be greater than zero"));

        // --- Validate InitiatorIdentityId ---
        var initiatorIdRaw = context.Data.GetValueOrDefault("initiatorIdentityId") as string;
        if (string.IsNullOrEmpty(initiatorIdRaw))
            return Task.FromResult(EngineResult.Fail("Missing initiatorIdentityId"));
        if (!Guid.TryParse(initiatorIdRaw, out var initiatorIdentityId) || initiatorIdentityId == Guid.Empty)
            return Task.FromResult(EngineResult.Fail("Invalid initiatorIdentityId format"));

        // --- Optional fields ---
        var referenceId = context.Data.GetValueOrDefault("referenceId") as string ?? "";
        var referenceType = context.Data.GetValueOrDefault("referenceType") as string ?? "";

        // --- Evaluate purpose compatibility ---
        var allowed = AllowedTransactions.TryGetValue(vaultPurpose, out var allowedTypes)
            && Array.Exists(allowedTypes, t => t == transactionType);

        var evaluatedAt = DateTime.UtcNow;

        var validationReason = allowed
            ? $"Transaction type '{transactionType}' is permitted for vault purpose '{vaultPurpose}'"
            : $"Transaction type '{transactionType}' is restricted for vault purpose '{vaultPurpose}'";

        // --- Emit validation event ---
        var validationEventType = allowed
            ? "VaultPurposeValidationPassed"
            : "VaultPurposeValidationFailed";

        var events = new[]
        {
            EngineEvent.Create("VaultPurposeValidationRequested", vaultId,
                new Dictionary<string, object>
                {
                    ["vaultId"] = vaultIdRaw,
                    ["vaultPurpose"] = vaultPurpose,
                    ["transactionType"] = transactionType,
                    ["amount"] = amount.Value,
                    ["initiatorIdentityId"] = initiatorIdRaw,
                    ["referenceId"] = referenceId,
                    ["referenceType"] = referenceType,
                    ["topic"] = "whyce.economic.events"
                }),

            EngineEvent.Create(validationEventType, vaultId,
                new Dictionary<string, object>
                {
                    ["vaultId"] = vaultIdRaw,
                    ["vaultPurpose"] = vaultPurpose,
                    ["transactionType"] = transactionType,
                    ["isAllowed"] = allowed,
                    ["validationReason"] = validationReason,
                    ["evaluatedAt"] = evaluatedAt.ToString("O"),
                    ["topic"] = "whyce.economic.events"
                })
        };

        var output = new Dictionary<string, object>
        {
            ["vaultId"] = vaultIdRaw,
            ["vaultPurpose"] = vaultPurpose,
            ["transactionType"] = transactionType,
            ["isAllowed"] = allowed,
            ["validationReason"] = validationReason,
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
