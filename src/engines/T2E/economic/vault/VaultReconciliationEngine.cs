namespace Whycespace.Engines.T2E.Economic.Vault;

using Whycespace.Contracts.Engines;
using Whycespace.Runtime.EngineManifest.Attributes;
using Whycespace.Runtime.EngineManifest.Models;

[EngineManifest("VaultReconciliation", EngineTier.T2E, EngineKind.Validation, "ExecuteVaultReconciliationCommand", typeof(EngineEvent))]
public sealed class VaultReconciliationEngine : IEngine
{
    public string Name => "VaultReconciliation";

    private static readonly string[] ValidScopes = { "ledger", "transaction", "full" };

    private static readonly string[] ValidTransactionTypes =
    {
        "Contribution", "Transfer", "Withdrawal", "Distribution", "ProfitDistribution",
        "Adjustment", "Refund"
    };

    public Task<EngineResult> ExecuteAsync(EngineContext context)
    {
        // --- Validate ReconciliationId ---
        var reconciliationIdRaw = context.Data.GetValueOrDefault("reconciliationId") as string;
        if (string.IsNullOrEmpty(reconciliationIdRaw))
            return Task.FromResult(EngineResult.Fail("Missing reconciliationId"));
        if (!Guid.TryParse(reconciliationIdRaw, out var reconciliationId) || reconciliationId == Guid.Empty)
            return Task.FromResult(EngineResult.Fail("Invalid reconciliationId format"));

        // --- Validate VaultId ---
        var vaultIdRaw = context.Data.GetValueOrDefault("vaultId") as string;
        if (string.IsNullOrEmpty(vaultIdRaw))
            return Task.FromResult(EngineResult.Fail("Missing vaultId"));
        if (!Guid.TryParse(vaultIdRaw, out var vaultId) || vaultId == Guid.Empty)
            return Task.FromResult(EngineResult.Fail("Invalid vaultId format"));

        // --- Validate ReconciliationScope ---
        var reconciliationScope = context.Data.GetValueOrDefault("reconciliationScope") as string;
        if (string.IsNullOrEmpty(reconciliationScope))
            return Task.FromResult(EngineResult.Fail("Missing reconciliationScope"));
        if (!Array.Exists(ValidScopes, s => s == reconciliationScope))
            return Task.FromResult(EngineResult.Fail($"Invalid reconciliationScope: {reconciliationScope}. Valid: {string.Join(", ", ValidScopes)}"));

        // --- Validate RequestedBy ---
        var requestedByRaw = context.Data.GetValueOrDefault("requestedBy") as string;
        if (string.IsNullOrEmpty(requestedByRaw))
            return Task.FromResult(EngineResult.Fail("Missing requestedBy"));
        if (!Guid.TryParse(requestedByRaw, out var requestedBy) || requestedBy == Guid.Empty)
            return Task.FromResult(EngineResult.Fail("Invalid requestedBy format"));

        // --- Optional fields ---
        var referenceId = context.Data.GetValueOrDefault("referenceId") as string ?? "";
        var referenceType = context.Data.GetValueOrDefault("referenceType") as string ?? "";

        // --- Emit reconciliation started event ---
        var startedEvent = EngineEvent.Create("VaultReconciliationStarted", vaultId,
            new Dictionary<string, object>
            {
                ["reconciliationId"] = reconciliationIdRaw,
                ["vaultId"] = vaultIdRaw,
                ["reconciliationScope"] = reconciliationScope,
                ["requestedBy"] = requestedByRaw,
                ["referenceId"] = referenceId,
                ["referenceType"] = referenceType,
                ["topic"] = "whyce.economic.events"
            });

        // --- Retrieve ledger entries from context ---
        var ledgerEntries = context.Data.GetValueOrDefault("ledgerEntries") as IReadOnlyList<object>;
        var transactions = context.Data.GetValueOrDefault("transactions") as IReadOnlyList<object>;
        var reportedLedgerBalance = ResolveDecimal(context.Data.GetValueOrDefault("ledgerBalance"));

        // --- Compute credit and debit totals from ledger entries ---
        decimal totalCredits = 0m;
        decimal totalDebits = 0m;
        var anomalies = new List<string>();
        var processedTransactionIds = new HashSet<string>();

        if (ledgerEntries is not null)
        {
            foreach (var entry in ledgerEntries)
            {
                if (entry is not IReadOnlyDictionary<string, object> ledgerEntry)
                {
                    anomalies.Add("Invalid ledger entry format detected");
                    continue;
                }

                var entryType = ledgerEntry.GetValueOrDefault("entryType") as string;
                var entryAmount = ResolveDecimal(ledgerEntry.GetValueOrDefault("amount"));
                var transactionId = ledgerEntry.GetValueOrDefault("transactionId") as string;

                if (string.IsNullOrEmpty(entryType) || entryAmount is null)
                {
                    anomalies.Add("Missing entryType or amount in ledger entry");
                    continue;
                }

                if (entryType == "Credit")
                    totalCredits += entryAmount.Value;
                else if (entryType == "Debit")
                    totalDebits += entryAmount.Value;
                else
                    anomalies.Add($"Unknown entry type: {entryType}");

                // Track ledger entries linked to transactions
                if (!string.IsNullOrEmpty(transactionId))
                {
                    if (!processedTransactionIds.Add(transactionId))
                        anomalies.Add($"Duplicate ledger entry for transaction: {transactionId}");
                }
            }
        }

        // --- Verify transaction consistency ---
        if (transactions is not null && (reconciliationScope == "transaction" || reconciliationScope == "full"))
        {
            foreach (var txn in transactions)
            {
                if (txn is not IReadOnlyDictionary<string, object> transaction)
                {
                    anomalies.Add("Invalid transaction format detected");
                    continue;
                }

                var txnId = transaction.GetValueOrDefault("transactionId") as string;
                var txnType = transaction.GetValueOrDefault("transactionType") as string;

                if (string.IsNullOrEmpty(txnId))
                {
                    anomalies.Add("Transaction missing transactionId");
                    continue;
                }

                if (!string.IsNullOrEmpty(txnType) && !Array.Exists(ValidTransactionTypes, t => t == txnType))
                    anomalies.Add($"Invalid transaction reference: {txnId} has unknown type '{txnType}'");

                // Verify matching ledger entries exist
                if (!processedTransactionIds.Contains(txnId))
                    anomalies.Add($"Missing ledger entry for transaction: {txnId}");
            }
        }

        // --- Compute balance ---
        var computedBalance = totalCredits - totalDebits;
        var ledgerBalance = reportedLedgerBalance ?? computedBalance;

        // --- Compare balances ---
        var isBalanced = computedBalance == ledgerBalance && anomalies.Count == 0;

        if (computedBalance != ledgerBalance)
            anomalies.Add($"Balance mismatch: ledger reports {ledgerBalance} but computed {computedBalance}");

        var completedAt = DateTime.UtcNow;
        var reconciliationStatus = isBalanced ? "Passed" : "Failed";
        var reconciliationNotes = anomalies.Count == 0
            ? "All ledger entries and transactions reconciled successfully"
            : string.Join("; ", anomalies);

        // --- Emit completion or failure event ---
        var completionEventType = isBalanced
            ? "VaultReconciliationCompleted"
            : "VaultReconciliationFailed";

        var events = new List<EngineEvent> { startedEvent };

        // Emit anomaly events
        foreach (var anomaly in anomalies)
        {
            events.Add(EngineEvent.Create("VaultReconciliationAnomalyDetected", vaultId,
                new Dictionary<string, object>
                {
                    ["reconciliationId"] = reconciliationIdRaw,
                    ["vaultId"] = vaultIdRaw,
                    ["anomaly"] = anomaly,
                    ["topic"] = "whyce.economic.events"
                }));
        }

        events.Add(EngineEvent.Create(completionEventType, vaultId,
            new Dictionary<string, object>
            {
                ["reconciliationId"] = reconciliationIdRaw,
                ["vaultId"] = vaultIdRaw,
                ["isBalanced"] = isBalanced,
                ["totalCredits"] = totalCredits,
                ["totalDebits"] = totalDebits,
                ["ledgerBalance"] = ledgerBalance,
                ["computedBalance"] = computedBalance,
                ["reconciliationStatus"] = reconciliationStatus,
                ["reconciliationNotes"] = reconciliationNotes,
                ["completedAt"] = completedAt.ToString("O"),
                ["topic"] = "whyce.economic.events"
            }));

        var output = new Dictionary<string, object>
        {
            ["reconciliationId"] = reconciliationIdRaw,
            ["vaultId"] = vaultIdRaw,
            ["isBalanced"] = isBalanced,
            ["totalCredits"] = totalCredits,
            ["totalDebits"] = totalDebits,
            ["ledgerBalance"] = ledgerBalance,
            ["computedBalance"] = computedBalance,
            ["reconciliationStatus"] = reconciliationStatus,
            ["reconciliationNotes"] = reconciliationNotes,
            ["completedAt"] = completedAt.ToString("O")
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
