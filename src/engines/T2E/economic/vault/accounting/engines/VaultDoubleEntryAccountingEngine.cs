namespace Whycespace.Engines.T2E.Economic.Vault.Accounting.Engines;

using Whycespace.Contracts.Engines;
using Whycespace.Runtime.EngineManifest.Attributes;
using Whycespace.Runtime.EngineManifest.Models;

[EngineManifest("VaultDoubleEntryAccounting", EngineTier.T2E, EngineKind.Validation, "ValidateDoubleEntryCommand", typeof(EngineEvent))]
public sealed class VaultDoubleEntryAccountingEngine : IEngine
{
    public string Name => "VaultDoubleEntryAccounting";

    private static readonly string[] ValidDirections = { "Debit", "Credit" };

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
        if (!Guid.TryParse(vaultIdRaw, out _))
            return Task.FromResult(EngineResult.Fail("Invalid vaultId format"));

        // --- Validate TransactionType ---
        var transactionType = context.Data.GetValueOrDefault("transactionType") as string;
        if (string.IsNullOrEmpty(transactionType))
            return Task.FromResult(EngineResult.Fail("Missing transactionType"));

        // --- Extract ledger entries ---
        var entriesRaw = context.Data.GetValueOrDefault("ledgerEntries");
        if (entriesRaw is not IList<Dictionary<string, object>> entries || entries.Count == 0)
            return Task.FromResult(EngineResult.Fail("Missing or empty ledgerEntries"));

        // --- Validate and aggregate ---
        var totalDebits = 0m;
        var totalCredits = 0m;

        foreach (var entry in entries)
        {
            var direction = entry.GetValueOrDefault("direction") as string;
            if (string.IsNullOrEmpty(direction) || !Array.Exists(ValidDirections, d => d == direction))
                return Task.FromResult(EngineResult.Fail($"Invalid ledger entry direction: {direction}"));

            var amount = ResolveDecimal(entry.GetValueOrDefault("amount"));
            if (amount is null || amount.Value <= 0)
                return Task.FromResult(EngineResult.Fail("Ledger entry amount must be greater than zero"));

            if (direction == "Debit")
                totalDebits += amount.Value;
            else
                totalCredits += amount.Value;
        }

        // --- Evaluate balance ---
        var isBalanced = totalDebits == totalCredits;
        var validationStatus = isBalanced ? "Passed" : "Failed";
        var validationReason = isBalanced
            ? "Total debits equal total credits"
            : $"Imbalance detected: debits={totalDebits}, credits={totalCredits}";
        var evaluatedAt = DateTime.UtcNow;

        // --- Emit events ---
        var requestedEvent = EngineEvent.Create("VaultDoubleEntryValidationRequested", transactionId,
            new Dictionary<string, object>
            {
                ["transactionId"] = transactionIdRaw,
                ["vaultId"] = vaultIdRaw,
                ["transactionType"] = transactionType,
                ["entryCount"] = entries.Count,
                ["topic"] = "whyce.economic.events"
            });

        var resultEventType = isBalanced
            ? "VaultDoubleEntryValidationPassed"
            : "VaultDoubleEntryValidationFailed";

        var resultEvent = EngineEvent.Create(resultEventType, transactionId,
            new Dictionary<string, object>
            {
                ["transactionId"] = transactionIdRaw,
                ["vaultId"] = vaultIdRaw,
                ["totalDebits"] = totalDebits,
                ["totalCredits"] = totalCredits,
                ["isBalanced"] = isBalanced,
                ["validationStatus"] = validationStatus,
                ["validationReason"] = validationReason,
                ["topic"] = "whyce.economic.events"
            });

        var events = new[] { requestedEvent, resultEvent };

        var output = new Dictionary<string, object>
        {
            ["transactionId"] = transactionIdRaw,
            ["isBalanced"] = isBalanced,
            ["totalDebits"] = totalDebits,
            ["totalCredits"] = totalCredits,
            ["validationStatus"] = validationStatus,
            ["validationReason"] = validationReason
        };

        if (!isBalanced)
            return Task.FromResult(EngineResult.Fail(validationReason));

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
