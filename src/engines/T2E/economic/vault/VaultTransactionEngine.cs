namespace Whycespace.Engines.T2E.Economic.Vault;

using Whycespace.Contracts.Engines;
using Whycespace.Runtime.EngineManifest.Attributes;
using Whycespace.Runtime.EngineManifest.Models;

[EngineManifest("VaultTransaction", EngineTier.T2E, EngineKind.Mutation, "VaultTransactionRequest", typeof(EngineEvent))]
public sealed class VaultTransactionEngine : IEngine
{
    public string Name => "VaultTransaction";

    private static readonly string[] SupportedCurrencies = { "GBP", "USD", "EUR", "NGN" };

    private static readonly string[] ValidTransactionTypes =
    {
        "Contribution", "Transfer", "Withdrawal", "Distribution", "Adjustment", "Refund"
    };

    public Task<EngineResult> ExecuteAsync(EngineContext context)
    {
        // --- Validate TransactionId ---
        var transactionIdRaw = context.Data.GetValueOrDefault("transactionId") as string;
        if (string.IsNullOrEmpty(transactionIdRaw))
            return Task.FromResult(EngineResult.Fail("Missing transactionId"));
        if (!Guid.TryParse(transactionIdRaw, out var transactionId))
            return Task.FromResult(EngineResult.Fail("Invalid transactionId format"));

        // --- Validate VaultId ---
        var vaultIdRaw = context.Data.GetValueOrDefault("vaultId") as string;
        if (string.IsNullOrEmpty(vaultIdRaw))
            return Task.FromResult(EngineResult.Fail("Missing vaultId"));
        if (!Guid.TryParse(vaultIdRaw, out var vaultId))
            return Task.FromResult(EngineResult.Fail("Invalid vaultId format"));

        // --- Validate VaultAccountId ---
        var vaultAccountIdRaw = context.Data.GetValueOrDefault("vaultAccountId") as string;
        if (string.IsNullOrEmpty(vaultAccountIdRaw))
            return Task.FromResult(EngineResult.Fail("Missing vaultAccountId"));
        if (!Guid.TryParse(vaultAccountIdRaw, out var vaultAccountId))
            return Task.FromResult(EngineResult.Fail("Invalid vaultAccountId format"));

        // --- Validate InitiatorIdentityId ---
        var initiatorIdRaw = context.Data.GetValueOrDefault("initiatorIdentityId") as string;
        if (string.IsNullOrEmpty(initiatorIdRaw))
            return Task.FromResult(EngineResult.Fail("Missing initiatorIdentityId"));
        if (!Guid.TryParse(initiatorIdRaw, out var initiatorIdentityId))
            return Task.FromResult(EngineResult.Fail("Invalid initiatorIdentityId format"));

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

        // --- Validate Currency ---
        var currency = context.Data.GetValueOrDefault("currency") as string;
        if (string.IsNullOrEmpty(currency))
            return Task.FromResult(EngineResult.Fail("Missing currency"));
        if (!Array.Exists(SupportedCurrencies, c => c == currency))
            return Task.FromResult(EngineResult.Fail($"Unsupported currency: {currency}. Supported: {string.Join(", ", SupportedCurrencies)}"));

        // --- Optional fields ---
        var description = context.Data.GetValueOrDefault("description") as string;
        var referenceId = context.Data.GetValueOrDefault("referenceId") as string ?? "";
        var referenceType = context.Data.GetValueOrDefault("referenceType") as string ?? "";

        // --- Determine ledger direction ---
        var ledgerDirection = transactionType switch
        {
            "Contribution" => "Credit",
            "Transfer" => "Debit",
            "Withdrawal" => "Debit",
            "Distribution" => "Debit",
            "Adjustment" => "Credit",
            "Refund" => "Credit",
            _ => "Credit"
        };

        // --- Emit lifecycle events ---
        var events = new[]
        {
            // Transaction created
            EngineEvent.Create("VaultTransactionCreated", transactionId,
                new Dictionary<string, object>
                {
                    ["transactionId"] = transactionIdRaw,
                    ["vaultId"] = vaultIdRaw,
                    ["vaultAccountId"] = vaultAccountIdRaw,
                    ["initiatorIdentityId"] = initiatorIdRaw,
                    ["transactionType"] = transactionType,
                    ["amount"] = amount.Value,
                    ["currency"] = currency,
                    ["description"] = description ?? "",
                    ["referenceId"] = referenceId,
                    ["referenceType"] = referenceType,
                    ["topic"] = "whyce.economic.events"
                }),

            // Transaction authorized
            EngineEvent.Create("VaultTransactionAuthorized", transactionId,
                new Dictionary<string, object>
                {
                    ["transactionId"] = transactionIdRaw,
                    ["vaultId"] = vaultIdRaw,
                    ["topic"] = "whyce.economic.events"
                }),

            // Transaction processing
            EngineEvent.Create("VaultTransactionProcessing", transactionId,
                new Dictionary<string, object>
                {
                    ["transactionId"] = transactionIdRaw,
                    ["vaultId"] = vaultIdRaw,
                    ["topic"] = "whyce.economic.events"
                }),

            // Ledger entry appended
            EngineEvent.Create("VaultLedgerEntryAppended", vaultId,
                new Dictionary<string, object>
                {
                    ["transactionId"] = transactionIdRaw,
                    ["vaultId"] = vaultIdRaw,
                    ["vaultAccountId"] = vaultAccountIdRaw,
                    ["amount"] = amount.Value,
                    ["currency"] = currency,
                    ["direction"] = ledgerDirection,
                    ["transactionType"] = transactionType,
                    ["referenceId"] = referenceId,
                    ["referenceType"] = referenceType,
                    ["topic"] = "whyce.economic.events"
                }),

            // Transaction registered in registry
            EngineEvent.Create("VaultTransactionRegistered", transactionId,
                new Dictionary<string, object>
                {
                    ["transactionId"] = transactionIdRaw,
                    ["vaultId"] = vaultIdRaw,
                    ["vaultAccountId"] = vaultAccountIdRaw,
                    ["initiatorIdentityId"] = initiatorIdRaw,
                    ["transactionType"] = transactionType,
                    ["amount"] = amount.Value,
                    ["currency"] = currency,
                    ["topic"] = "whyce.economic.events"
                }),

            // Transaction completed
            EngineEvent.Create("VaultTransactionCompleted", transactionId,
                new Dictionary<string, object>
                {
                    ["transactionId"] = transactionIdRaw,
                    ["vaultId"] = vaultIdRaw,
                    ["vaultAccountId"] = vaultAccountIdRaw,
                    ["transactionType"] = transactionType,
                    ["amount"] = amount.Value,
                    ["currency"] = currency,
                    ["status"] = "Completed",
                    ["topic"] = "whyce.economic.events"
                })
        };

        var output = new Dictionary<string, object>
        {
            ["transactionId"] = transactionIdRaw,
            ["vaultId"] = vaultIdRaw,
            ["vaultAccountId"] = vaultAccountIdRaw,
            ["transactionType"] = transactionType,
            ["amount"] = amount.Value,
            ["currency"] = currency,
            ["status"] = "Completed",
            ["ledgerDirection"] = ledgerDirection
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
