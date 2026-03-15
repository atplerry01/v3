namespace Whycespace.Engines.T2E.Economic.Vault;

using Whycespace.Contracts.Engines;
using Whycespace.Runtime.EngineManifest.Attributes;
using Whycespace.Runtime.EngineManifest.Models;

[EngineManifest("VaultReplay", EngineTier.T2E, EngineKind.Mutation, "ExecuteVaultReplayCommand", typeof(EngineEvent))]
public sealed class VaultReplayEngine : IEngine
{
    public string Name => "VaultReplay";

    public Task<EngineResult> ExecuteAsync(EngineContext context)
    {
        // --- Validate ReplayId ---
        var replayIdRaw = context.Data.GetValueOrDefault("replayId") as string;
        if (string.IsNullOrEmpty(replayIdRaw))
            return Task.FromResult(EngineResult.Fail("Missing replayId"));
        if (!Guid.TryParse(replayIdRaw, out var replayId) || replayId == Guid.Empty)
            return Task.FromResult(EngineResult.Fail("Invalid replayId format"));

        // --- Validate VaultId ---
        var vaultIdRaw = context.Data.GetValueOrDefault("vaultId") as string;
        if (string.IsNullOrEmpty(vaultIdRaw))
            return Task.FromResult(EngineResult.Fail("Missing vaultId"));
        if (!Guid.TryParse(vaultIdRaw, out var vaultId) || vaultId == Guid.Empty)
            return Task.FromResult(EngineResult.Fail("Invalid vaultId format"));

        // --- Validate SnapshotId ---
        var snapshotIdRaw = context.Data.GetValueOrDefault("snapshotId") as string;
        if (string.IsNullOrEmpty(snapshotIdRaw))
            return Task.FromResult(EngineResult.Fail("Missing snapshotId"));
        if (!Guid.TryParse(snapshotIdRaw, out var snapshotId) || snapshotId == Guid.Empty)
            return Task.FromResult(EngineResult.Fail("Invalid snapshotId format"));

        // --- Validate RequestedBy ---
        var requestedByRaw = context.Data.GetValueOrDefault("requestedBy") as string;
        if (string.IsNullOrEmpty(requestedByRaw))
            return Task.FromResult(EngineResult.Fail("Missing requestedBy"));
        if (!Guid.TryParse(requestedByRaw, out var requestedBy) || requestedBy == Guid.Empty)
            return Task.FromResult(EngineResult.Fail("Invalid requestedBy format"));

        // --- Validate timestamps ---
        var replayStartRaw = context.Data.GetValueOrDefault("replayStartTimestamp") as string;
        var replayEndRaw = context.Data.GetValueOrDefault("replayEndTimestamp") as string;
        if (string.IsNullOrEmpty(replayStartRaw))
            return Task.FromResult(EngineResult.Fail("Missing replayStartTimestamp"));
        if (string.IsNullOrEmpty(replayEndRaw))
            return Task.FromResult(EngineResult.Fail("Missing replayEndTimestamp"));
        if (!DateTime.TryParse(replayStartRaw, out var replayStart))
            return Task.FromResult(EngineResult.Fail("Invalid replayStartTimestamp format"));
        if (!DateTime.TryParse(replayEndRaw, out var replayEnd))
            return Task.FromResult(EngineResult.Fail("Invalid replayEndTimestamp format"));
        if (replayEnd <= replayStart)
            return Task.FromResult(EngineResult.Fail("replayEndTimestamp must be after replayStartTimestamp"));

        // --- Optional fields ---
        var replayScope = context.Data.GetValueOrDefault("replayScope") as string ?? "full";
        var referenceId = context.Data.GetValueOrDefault("referenceId") as string ?? "";

        // --- Load snapshot baseline ---
        var snapshotBalance = ResolveDecimal(context.Data.GetValueOrDefault("snapshotBalance")) ?? 0m;

        // --- Emit replay started event ---
        var events = new List<EngineEvent>
        {
            EngineEvent.Create("VaultReplayStarted", vaultId,
                new Dictionary<string, object>
                {
                    ["replayId"] = replayIdRaw,
                    ["vaultId"] = vaultIdRaw,
                    ["snapshotId"] = snapshotIdRaw,
                    ["snapshotBalance"] = snapshotBalance,
                    ["replayStartTimestamp"] = replayStartRaw,
                    ["replayEndTimestamp"] = replayEndRaw,
                    ["replayScope"] = replayScope,
                    ["requestedBy"] = requestedByRaw,
                    ["topic"] = "whyce.economic.events"
                })
        };

        // --- Retrieve ledger entries from context ---
        var ledgerEntries = context.Data.GetValueOrDefault("ledgerEntries") as IReadOnlyList<object>;

        // --- Sort and replay ledger entries chronologically ---
        decimal currentBalance = snapshotBalance;
        int replayedLedgerEntryCount = 0;
        int replayedTransactionCount = 0;
        var processedTransactionIds = new HashSet<string>();

        if (ledgerEntries is not null)
        {
            // Build sortable list with timestamps
            var sortableEntries = new List<(int index, DateTime timestamp, IReadOnlyDictionary<string, object> entry)>();

            for (int i = 0; i < ledgerEntries.Count; i++)
            {
                if (ledgerEntries[i] is not IReadOnlyDictionary<string, object> entry)
                    continue;

                var timestampRaw = entry.GetValueOrDefault("timestamp") as string;
                var entryTimestamp = DateTime.MinValue;
                if (!string.IsNullOrEmpty(timestampRaw))
                    DateTime.TryParse(timestampRaw, out entryTimestamp);

                // Filter to entries within replay window
                if (entryTimestamp >= replayStart && entryTimestamp <= replayEnd)
                    sortableEntries.Add((i, entryTimestamp, entry));
            }

            // Sort chronologically
            sortableEntries.Sort((a, b) => a.timestamp.CompareTo(b.timestamp));

            // Replay in order
            foreach (var (_, _, entry) in sortableEntries)
            {
                var direction = entry.GetValueOrDefault("direction") as string;
                var amount = ResolveDecimal(entry.GetValueOrDefault("amount"));

                if (string.IsNullOrEmpty(direction) || amount is null)
                    continue;

                if (direction == "Credit")
                    currentBalance += amount.Value;
                else if (direction == "Debit")
                    currentBalance -= amount.Value;

                replayedLedgerEntryCount++;

                // Track unique transactions
                var transactionId = entry.GetValueOrDefault("transactionId") as string;
                if (!string.IsNullOrEmpty(transactionId))
                    processedTransactionIds.Add(transactionId);
            }
        }

        replayedTransactionCount = processedTransactionIds.Count;

        var completedAt = DateTime.UtcNow;
        var replayStatus = "Completed";
        var replayNotes = $"Replayed {replayedLedgerEntryCount} ledger entries and {replayedTransactionCount} transactions from snapshot baseline";

        // --- Emit replay completed event ---
        events.Add(EngineEvent.Create("VaultReplayCompleted", vaultId,
            new Dictionary<string, object>
            {
                ["replayId"] = replayIdRaw,
                ["vaultId"] = vaultIdRaw,
                ["snapshotId"] = snapshotIdRaw,
                ["finalVaultBalance"] = currentBalance,
                ["replayedTransactionCount"] = replayedTransactionCount,
                ["replayedLedgerEntryCount"] = replayedLedgerEntryCount,
                ["replayStatus"] = replayStatus,
                ["replayNotes"] = replayNotes,
                ["completedAt"] = completedAt.ToString("O"),
                ["topic"] = "whyce.economic.events"
            }));

        var output = new Dictionary<string, object>
        {
            ["replayId"] = replayIdRaw,
            ["vaultId"] = vaultIdRaw,
            ["snapshotId"] = snapshotIdRaw,
            ["finalVaultBalance"] = currentBalance,
            ["replayedTransactionCount"] = replayedTransactionCount,
            ["replayedLedgerEntryCount"] = replayedLedgerEntryCount,
            ["replayStatus"] = replayStatus,
            ["replayNotes"] = replayNotes,
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
