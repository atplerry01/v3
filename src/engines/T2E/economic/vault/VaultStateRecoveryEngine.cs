namespace Whycespace.Engines.T2E.Economic.Vault;

using Whycespace.Contracts.Engines;
using Whycespace.Runtime.EngineManifest.Attributes;
using Whycespace.Runtime.EngineManifest.Models;

[EngineManifest("VaultStateRecovery", EngineTier.T2E, EngineKind.Mutation, "ExecuteVaultRecoveryCommand", typeof(EngineEvent))]
public sealed class VaultStateRecoveryEngine : IEngine
{
    public string Name => "VaultStateRecovery";

    private static readonly string[] ValidRecoveryScopes = { "full", "balance", "participants", "allocations" };

    public Task<EngineResult> ExecuteAsync(EngineContext context)
    {
        // --- Validate RecoveryId ---
        var recoveryIdRaw = context.Data.GetValueOrDefault("recoveryId") as string;
        if (string.IsNullOrEmpty(recoveryIdRaw))
            return Task.FromResult(EngineResult.Fail("Missing recoveryId"));
        if (!Guid.TryParse(recoveryIdRaw, out var recoveryId) || recoveryId == Guid.Empty)
            return Task.FromResult(EngineResult.Fail("Invalid recoveryId format"));

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

        // --- Optional fields ---
        var recoveryScope = context.Data.GetValueOrDefault("recoveryScope") as string ?? "full";
        if (!Array.Exists(ValidRecoveryScopes, s => s == recoveryScope))
            return Task.FromResult(EngineResult.Fail($"Invalid recoveryScope: {recoveryScope}. Valid: {string.Join(", ", ValidRecoveryScopes)}"));

        var referenceId = context.Data.GetValueOrDefault("referenceId") as string ?? "";

        // --- Emit recovery started event ---
        var events = new List<EngineEvent>();
        events.Add(EngineEvent.Create("VaultRecoveryStarted", vaultId,
            new Dictionary<string, object>
            {
                ["recoveryId"] = recoveryIdRaw,
                ["vaultId"] = vaultIdRaw,
                ["snapshotId"] = snapshotIdRaw,
                ["recoveryScope"] = recoveryScope,
                ["requestedBy"] = requestedByRaw,
                ["referenceId"] = referenceId,
                ["topic"] = "whyce.economic.events"
            }));

        // --- Load snapshot data from context ---
        var snapshotData = context.Data.GetValueOrDefault("snapshotData") as IReadOnlyDictionary<string, object>;
        if (snapshotData is null)
            return Task.FromResult(EngineResult.Fail("Missing snapshotData"));

        // --- Restore vault balance ---
        var snapshotBalance = ResolveDecimal(snapshotData.GetValueOrDefault("vaultBalance"));
        if (snapshotBalance is null)
            return Task.FromResult(EngineResult.Fail("Missing vaultBalance in snapshot data"));

        var recoveredBalance = snapshotBalance.Value;

        // --- Restore transaction count ---
        var transactionCount = ResolveInt(snapshotData.GetValueOrDefault("transactionCount"));
        var recoveredTransactionCount = transactionCount ?? 0;

        // --- Restore participant state ---
        var participants = snapshotData.GetValueOrDefault("participants") as IReadOnlyList<object>;
        var recoveredParticipantCount = participants?.Count ?? 0;

        // --- Restore allocation state ---
        var allocations = snapshotData.GetValueOrDefault("allocations") as IReadOnlyList<object>;
        var recoveredAllocationCount = allocations?.Count ?? 0;

        // --- Restore snapshot timestamp ---
        var snapshotTimestamp = snapshotData.GetValueOrDefault("snapshotTimestamp") as string ?? "";

        var completedAt = DateTime.UtcNow;
        var recoveryStatus = "Recovered";
        var recoveryNotes = $"Vault state restored from snapshot. Balance: {recoveredBalance}, Transactions: {recoveredTransactionCount}, Participants: {recoveredParticipantCount}, Allocations: {recoveredAllocationCount}";

        // --- Emit recovery completed event ---
        events.Add(EngineEvent.Create("VaultRecoveryCompleted", vaultId,
            new Dictionary<string, object>
            {
                ["recoveryId"] = recoveryIdRaw,
                ["vaultId"] = vaultIdRaw,
                ["snapshotId"] = snapshotIdRaw,
                ["recoveredVaultBalance"] = recoveredBalance,
                ["recoveredTransactionCount"] = recoveredTransactionCount,
                ["recoveredParticipantCount"] = recoveredParticipantCount,
                ["recoveredAllocationCount"] = recoveredAllocationCount,
                ["recoveryStatus"] = recoveryStatus,
                ["recoveryNotes"] = recoveryNotes,
                ["snapshotTimestamp"] = snapshotTimestamp,
                ["completedAt"] = completedAt.ToString("O"),
                ["topic"] = "whyce.economic.events"
            }));

        var output = new Dictionary<string, object>
        {
            ["recoveryId"] = recoveryIdRaw,
            ["vaultId"] = vaultIdRaw,
            ["snapshotId"] = snapshotIdRaw,
            ["recoveredVaultBalance"] = recoveredBalance,
            ["recoveredTransactionCount"] = recoveredTransactionCount,
            ["recoveredParticipantCount"] = recoveredParticipantCount,
            ["recoveredAllocationCount"] = recoveredAllocationCount,
            ["recoveryStatus"] = recoveryStatus,
            ["recoveryNotes"] = recoveryNotes,
            ["snapshotTimestamp"] = snapshotTimestamp,
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

    private static int? ResolveInt(object? value)
    {
        return value switch
        {
            int i => i,
            long l => (int)l,
            decimal d => (int)d,
            double d => (int)d,
            string s when int.TryParse(s, out var parsed) => parsed,
            _ => null
        };
    }
}
