namespace Whycespace.Engines.T2E.Core.Vault;

using Whycespace.Contracts.Engines;
using Whycespace.Runtime.EngineManifest.Attributes;
using Whycespace.Runtime.EngineManifest.Models;

[EngineManifest("VaultBalance", EngineTier.T2E, EngineKind.Projection, "ComputeVaultBalanceCommand", typeof(EngineEvent))]
public sealed class VaultBalanceEngine : IEngine
{
    public string Name => "VaultBalance";

    private static readonly string[] SupportedCurrencies = { "GBP", "USD", "EUR", "NGN" };

    public Task<EngineResult> ExecuteAsync(EngineContext context)
    {
        // --- Validate VaultId ---
        var vaultIdStr = context.Data.GetValueOrDefault("vaultId") as string;
        if (string.IsNullOrEmpty(vaultIdStr))
            return Task.FromResult(EngineResult.Fail("Missing vaultId"));
        if (!Guid.TryParse(vaultIdStr, out var vaultId))
            return Task.FromResult(EngineResult.Fail("Invalid vaultId format"));

        // --- Validate VaultAccountId ---
        var vaultAccountIdStr = context.Data.GetValueOrDefault("vaultAccountId") as string;
        if (string.IsNullOrEmpty(vaultAccountIdStr))
            return Task.FromResult(EngineResult.Fail("Missing vaultAccountId"));
        if (!Guid.TryParse(vaultAccountIdStr, out var vaultAccountId))
            return Task.FromResult(EngineResult.Fail("Invalid vaultAccountId format"));

        // --- Validate Currency ---
        var currency = context.Data.GetValueOrDefault("currency") as string;
        if (string.IsNullOrEmpty(currency))
            return Task.FromResult(EngineResult.Fail("Missing currency"));
        if (!Array.Exists(SupportedCurrencies, c => c == currency))
            return Task.FromResult(EngineResult.Fail($"Unsupported currency: {currency}. Supported: {string.Join(", ", SupportedCurrencies)}"));

        // --- Optional fields ---
        var balanceScope = context.Data.GetValueOrDefault("balanceScope") as string ?? "Current";

        // --- Resolve ledger entries ---
        var ledgerEntries = context.Data.GetValueOrDefault("ledgerEntries") as IEnumerable<IReadOnlyDictionary<string, object>>;

        decimal totalCredits = 0m;
        decimal totalDebits = 0m;

        if (ledgerEntries is not null)
        {
            foreach (var entry in ledgerEntries)
            {
                var entryAccountIdStr = entry.GetValueOrDefault("vaultAccountId") as string;
                if (entryAccountIdStr != vaultAccountIdStr)
                    continue;

                var entryCurrency = entry.GetValueOrDefault("currency") as string;
                if (entryCurrency != currency)
                    continue;

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

        var currentBalance = totalCredits - totalDebits;
        var calculatedAt = DateTimeOffset.UtcNow;

        // Event 1: VaultBalanceComputed — balance successfully calculated from ledger
        var computedEvent = EngineEvent.Create("VaultBalanceComputed", vaultId,
            new Dictionary<string, object>
            {
                ["vaultId"] = vaultId.ToString(),
                ["vaultAccountId"] = vaultAccountId.ToString(),
                ["totalCredits"] = totalCredits,
                ["totalDebits"] = totalDebits,
                ["currentBalance"] = currentBalance,
                ["currency"] = currency,
                ["balanceScope"] = balanceScope,
                ["calculatedAt"] = calculatedAt.ToString("O"),
                ["topic"] = "whyce.economic.events"
            });

        var events = new[] { computedEvent };

        var output = new Dictionary<string, object>
        {
            ["vaultId"] = vaultId.ToString(),
            ["vaultAccountId"] = vaultAccountId.ToString(),
            ["totalCredits"] = totalCredits,
            ["totalDebits"] = totalDebits,
            ["currentBalance"] = currentBalance,
            ["currency"] = currency,
            ["balanceScope"] = balanceScope,
            ["calculatedAt"] = calculatedAt.ToString("O")
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
