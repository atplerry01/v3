namespace Whycespace.Engines.T2E.Economic.Vault.Settlement.Engines;

using Whycespace.Contracts.Engines;
using Whycespace.Runtime.EngineManifest.Attributes;
using Whycespace.Runtime.EngineManifest.Models;

[EngineManifest("VaultSettlement", EngineTier.T2E, EngineKind.Mutation, "VaultSettlementRequest", typeof(EngineEvent))]
public sealed class VaultSettlementEngine : IEngine
{
    public string Name => "VaultSettlement";

    private static readonly string[] SupportedCurrencies = { "GBP", "USD", "EUR", "NGN" };

    private static readonly string[] ValidTransactionTypes =
    {
        "Contribution", "Transfer", "Withdrawal", "Distribution", "Adjustment", "Refund"
    };

    public Task<EngineResult> ExecuteAsync(EngineContext context)
    {
        // --- Validate SettlementId ---
        var settlementIdRaw = context.Data.GetValueOrDefault("settlementId") as string;
        if (string.IsNullOrEmpty(settlementIdRaw))
            return Task.FromResult(EngineResult.Fail("Missing settlementId"));
        if (!Guid.TryParse(settlementIdRaw, out var settlementId))
            return Task.FromResult(EngineResult.Fail("Invalid settlementId format"));

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

        // --- Validate RequestedBy ---
        var requestedByRaw = context.Data.GetValueOrDefault("requestedBy") as string;
        if (string.IsNullOrEmpty(requestedByRaw))
            return Task.FromResult(EngineResult.Fail("Missing requestedBy"));
        if (!Guid.TryParse(requestedByRaw, out var requestedBy))
            return Task.FromResult(EngineResult.Fail("Invalid requestedBy format"));

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

        // --- Validate TransactionType (from transaction record context) ---
        var transactionType = context.Data.GetValueOrDefault("transactionType") as string;
        if (string.IsNullOrEmpty(transactionType))
            return Task.FromResult(EngineResult.Fail("Missing transactionType"));
        if (!Array.Exists(ValidTransactionTypes, t => t == transactionType))
            return Task.FromResult(EngineResult.Fail($"Invalid transactionType: {transactionType}. Valid: {string.Join(", ", ValidTransactionTypes)}"));

        // --- Validate TransactionStatus (must not already be settled) ---
        var transactionStatus = context.Data.GetValueOrDefault("transactionStatus") as string;
        if (string.IsNullOrEmpty(transactionStatus))
            return Task.FromResult(EngineResult.Fail("Missing transactionStatus"));
        if (transactionStatus == "Settled")
            return Task.FromResult(EngineResult.Fail("Transaction is already settled"));
        if (transactionStatus != "Completed")
            return Task.FromResult(EngineResult.Fail($"Transaction cannot be settled in status: {transactionStatus}. Must be Completed"));

        // --- Validate LedgerEntryExists flag ---
        var ledgerEntryExists = context.Data.GetValueOrDefault("ledgerEntryExists");
        if (ledgerEntryExists is not true && ledgerEntryExists is not "true")
            return Task.FromResult(EngineResult.Fail("Ledger entries not found for transaction"));

        // --- Optional fields ---
        var settlementReference = context.Data.GetValueOrDefault("settlementReference") as string ?? "";
        var settlementScope = context.Data.GetValueOrDefault("settlementScope") as string ?? "";

        var settledAt = DateTimeOffset.UtcNow;

        // --- Emit settlement lifecycle events ---
        var events = new[]
        {
            // Settlement requested
            EngineEvent.Create("VaultSettlementRequested", settlementId,
                new Dictionary<string, object>
                {
                    ["settlementId"] = settlementIdRaw,
                    ["transactionId"] = transactionIdRaw,
                    ["vaultId"] = vaultIdRaw,
                    ["vaultAccountId"] = vaultAccountIdRaw,
                    ["requestedBy"] = requestedByRaw,
                    ["amount"] = amount.Value,
                    ["currency"] = currency,
                    ["transactionType"] = transactionType,
                    ["settlementReference"] = settlementReference,
                    ["settlementScope"] = settlementScope,
                    ["topic"] = "whyce.economic.events"
                }),

            // Settlement completed — transaction marked as settled
            EngineEvent.Create("VaultSettlementCompleted", settlementId,
                new Dictionary<string, object>
                {
                    ["settlementId"] = settlementIdRaw,
                    ["transactionId"] = transactionIdRaw,
                    ["vaultId"] = vaultIdRaw,
                    ["vaultAccountId"] = vaultAccountIdRaw,
                    ["amount"] = amount.Value,
                    ["currency"] = currency,
                    ["transactionType"] = transactionType,
                    ["settlementStatus"] = "Settled",
                    ["settledAt"] = settledAt.ToString("O"),
                    ["topic"] = "whyce.economic.events"
                })
        };

        var output = new Dictionary<string, object>
        {
            ["settlementId"] = settlementIdRaw,
            ["transactionId"] = transactionIdRaw,
            ["vaultId"] = vaultIdRaw,
            ["amount"] = amount.Value,
            ["currency"] = currency,
            ["isSettled"] = true,
            ["settlementStatus"] = "Settled",
            ["settledAt"] = settledAt.ToString("O")
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
