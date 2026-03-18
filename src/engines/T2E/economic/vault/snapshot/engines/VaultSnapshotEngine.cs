namespace Whycespace.Engines.T2E.Economic.Vault.Engines;

using Whycespace.Contracts.Engines;
using Whycespace.Runtime.EngineManifest.Attributes;
using Whycespace.Runtime.EngineManifest.Models;

[EngineManifest("VaultSnapshot", EngineTier.T2E, EngineKind.Projection, "CreateVaultSnapshotCommand", typeof(EngineEvent))]
public sealed class VaultSnapshotEngine : IEngine
{
    public string Name => "VaultSnapshot";

    public Task<EngineResult> ExecuteAsync(EngineContext context)
    {
        // --- Validate SnapshotId ---
        var snapshotIdRaw = context.Data.GetValueOrDefault("snapshotId") as string;
        if (string.IsNullOrEmpty(snapshotIdRaw))
            return Task.FromResult(EngineResult.Fail("Missing snapshotId"));
        if (!Guid.TryParse(snapshotIdRaw, out var snapshotId) || snapshotId == Guid.Empty)
            return Task.FromResult(EngineResult.Fail("Invalid snapshotId format"));

        // --- Validate VaultId ---
        var vaultIdRaw = context.Data.GetValueOrDefault("vaultId") as string;
        if (string.IsNullOrEmpty(vaultIdRaw))
            return Task.FromResult(EngineResult.Fail("Missing vaultId"));
        if (!Guid.TryParse(vaultIdRaw, out var vaultId) || vaultId == Guid.Empty)
            return Task.FromResult(EngineResult.Fail("Invalid vaultId format"));

        // --- Validate SnapshotTimestamp ---
        var snapshotTimestampRaw = context.Data.GetValueOrDefault("snapshotTimestamp") as string;
        if (string.IsNullOrEmpty(snapshotTimestampRaw))
            return Task.FromResult(EngineResult.Fail("Missing snapshotTimestamp"));
        if (!DateTime.TryParse(snapshotTimestampRaw, out var snapshotTimestamp))
            return Task.FromResult(EngineResult.Fail("Invalid snapshotTimestamp format"));

        // --- Validate RequestedBy ---
        var requestedByRaw = context.Data.GetValueOrDefault("requestedBy") as string;
        if (string.IsNullOrEmpty(requestedByRaw))
            return Task.FromResult(EngineResult.Fail("Missing requestedBy"));
        if (!Guid.TryParse(requestedByRaw, out var requestedBy) || requestedBy == Guid.Empty)
            return Task.FromResult(EngineResult.Fail("Invalid requestedBy format"));

        // --- Optional fields ---
        var snapshotScope = context.Data.GetValueOrDefault("snapshotScope") as string ?? "FullVault";
        var referenceId = context.Data.GetValueOrDefault("referenceId") as string ?? "";

        // --- Event 1: VaultSnapshotRequested ---
        var events = new List<EngineEvent>();
        events.Add(EngineEvent.Create("VaultSnapshotRequested", vaultId,
            new Dictionary<string, object>
            {
                ["snapshotId"] = snapshotIdRaw,
                ["vaultId"] = vaultIdRaw,
                ["snapshotTimestamp"] = snapshotTimestamp.ToString("O"),
                ["requestedBy"] = requestedByRaw,
                ["snapshotScope"] = snapshotScope,
                ["referenceId"] = referenceId,
                ["topic"] = "whyce.economic.events"
            }));

        // --- Compute vault balance from ledger entries ---
        var ledgerEntries = context.Data.GetValueOrDefault("ledgerEntries") as IEnumerable<IReadOnlyDictionary<string, object>>;

        decimal totalCredits = 0m;
        decimal totalDebits = 0m;

        if (ledgerEntries is not null)
        {
            foreach (var entry in ledgerEntries)
            {
                var amount = ResolveDecimal(entry.GetValueOrDefault("amount"));
                if (amount is null or <= 0)
                    continue;

                var direction = entry.GetValueOrDefault("direction") as string;
                switch (direction)
                {
                    case "Credit":
                        totalCredits += amount.Value;
                        break;
                    case "Debit":
                        totalDebits += amount.Value;
                        break;
                }
            }
        }

        var vaultBalance = totalCredits - totalDebits;

        // --- Count transactions ---
        var transactions = context.Data.GetValueOrDefault("transactions") as IReadOnlyList<object>;
        var transactionCount = transactions?.Count ?? 0;

        // --- Count participants ---
        var participants = context.Data.GetValueOrDefault("participants") as IReadOnlyList<object>;
        var participantCount = participants?.Count ?? 0;

        // --- Compute snapshot hash ---
        var hashInput = $"{snapshotIdRaw}:{vaultIdRaw}:{vaultBalance}:{transactionCount}:{participantCount}:{snapshotTimestamp:O}";
        var snapshotHash = ComputeHash(hashInput);

        var createdAt = DateTime.UtcNow;
        var snapshotStatus = "Created";

        // --- Event 2: VaultSnapshotCreated ---
        events.Add(EngineEvent.Create("VaultSnapshotCreated", vaultId,
            new Dictionary<string, object>
            {
                ["snapshotId"] = snapshotIdRaw,
                ["vaultId"] = vaultIdRaw,
                ["vaultBalance"] = vaultBalance,
                ["transactionCount"] = transactionCount,
                ["participantCount"] = participantCount,
                ["snapshotStatus"] = snapshotStatus,
                ["snapshotScope"] = snapshotScope,
                ["snapshotHash"] = snapshotHash,
                ["snapshotTimestamp"] = snapshotTimestamp.ToString("O"),
                ["createdAt"] = createdAt.ToString("O"),
                ["topic"] = "whyce.economic.events"
            }));

        var output = new Dictionary<string, object>
        {
            ["snapshotId"] = snapshotIdRaw,
            ["vaultId"] = vaultIdRaw,
            ["vaultBalance"] = vaultBalance,
            ["transactionCount"] = transactionCount,
            ["participantCount"] = participantCount,
            ["snapshotStatus"] = snapshotStatus,
            ["snapshotScope"] = snapshotScope,
            ["snapshotHash"] = snapshotHash,
            ["snapshotTimestamp"] = snapshotTimestamp.ToString("O"),
            ["createdAt"] = createdAt.ToString("O")
        };

        return Task.FromResult(EngineResult.Ok(events, output));
    }

    private static string ComputeHash(string input)
    {
        var bytes = global::System.Text.Encoding.UTF8.GetBytes(input);
        var hash = global::System.Security.Cryptography.SHA256.HashData(bytes);
        return Convert.ToHexStringLower(hash);
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