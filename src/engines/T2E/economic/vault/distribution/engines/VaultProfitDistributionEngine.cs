namespace Whycespace.Engines.T2E.Economic.Vault.Engines;

using Whycespace.Contracts.Engines;
using Whycespace.Runtime.EngineManifest.Attributes;
using Whycespace.Runtime.EngineManifest.Models;

[EngineManifest("VaultProfitDistribution", EngineTier.T2E, EngineKind.Mutation, "ExecuteProfitDistributionCommand", typeof(EngineEvent))]
public sealed class VaultProfitDistributionEngine : IEngine
{
    public string Name => "VaultProfitDistribution";

    private static readonly string[] SupportedCurrencies = { "GBP", "USD", "EUR", "NGN" };

    public Task<EngineResult> ExecuteAsync(EngineContext context)
    {
        // --- Validate DistributionId ---
        var distributionIdRaw = context.Data.GetValueOrDefault("distributionId") as string;
        if (string.IsNullOrEmpty(distributionIdRaw))
            return Task.FromResult(EngineResult.Fail("Missing distributionId"));
        if (!Guid.TryParse(distributionIdRaw, out var distributionId))
            return Task.FromResult(EngineResult.Fail("Invalid distributionId format"));

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

        // --- Validate TotalProfitAmount ---
        var totalProfitAmount = ResolveDecimal(context.Data.GetValueOrDefault("totalProfitAmount"));
        if (totalProfitAmount is null)
            return Task.FromResult(EngineResult.Fail("Missing or invalid totalProfitAmount"));
        if (totalProfitAmount.Value <= 0)
            return Task.FromResult(EngineResult.Fail("TotalProfitAmount must be greater than zero"));

        // --- Validate Currency ---
        var currency = context.Data.GetValueOrDefault("currency") as string;
        if (string.IsNullOrEmpty(currency))
            return Task.FromResult(EngineResult.Fail("Missing currency"));
        if (!Array.Exists(SupportedCurrencies, c => c == currency))
            return Task.FromResult(EngineResult.Fail($"Unsupported currency: {currency}. Supported: {string.Join(", ", SupportedCurrencies)}"));

        // --- Validate InitiatorIdentityId ---
        var initiatorIdRaw = context.Data.GetValueOrDefault("initiatorIdentityId") as string;
        if (string.IsNullOrEmpty(initiatorIdRaw))
            return Task.FromResult(EngineResult.Fail("Missing initiatorIdentityId"));
        if (!Guid.TryParse(initiatorIdRaw, out var initiatorIdentityId))
            return Task.FromResult(EngineResult.Fail("Invalid initiatorIdentityId format"));

        // --- Optional fields ---
        var distributionReference = context.Data.GetValueOrDefault("distributionReference") as string ?? "";
        var description = context.Data.GetValueOrDefault("description") as string ?? "";

        // --- Validate vault balance ---
        var vaultBalance = ResolveDecimal(context.Data.GetValueOrDefault("vaultBalance"));
        if (vaultBalance is not null && vaultBalance.Value < totalProfitAmount.Value)
            return Task.FromResult(EngineResult.Fail("Insufficient vault balance for distribution"));

        // --- Load and validate allocation structure ---
        var allocations = ResolveAllocations(context.Data);
        if (allocations is null || allocations.Count == 0)
            return Task.FromResult(EngineResult.Fail("Missing or empty allocation structure"));

        var totalPercentage = 0m;
        foreach (var allocation in allocations)
        {
            if (allocation.Percentage <= 0 || allocation.Percentage > 100)
                return Task.FromResult(EngineResult.Fail($"Invalid allocation percentage: {allocation.Percentage}"));
            if (allocation.RecipientId == Guid.Empty)
                return Task.FromResult(EngineResult.Fail("Invalid allocation recipient"));
            totalPercentage += allocation.Percentage;
        }

        if (totalPercentage > 100)
            return Task.FromResult(EngineResult.Fail($"Allocation percentages exceed 100%: {totalPercentage}%"));

        // --- Calculate participant distribution shares ---
        var participantShares = new List<(Guid RecipientId, decimal Amount)>();
        var distributedTotal = 0m;

        for (var i = 0; i < allocations.Count; i++)
        {
            var allocation = allocations[i];
            decimal share;

            if (i == allocations.Count - 1)
            {
                // Last participant gets remainder to avoid rounding drift
                share = totalProfitAmount.Value - distributedTotal;
            }
            else
            {
                share = Math.Round(totalProfitAmount.Value * allocation.Percentage / 100m, 2);
                distributedTotal += share;
            }

            participantShares.Add((allocation.RecipientId, share));
        }

        var completedAt = DateTimeOffset.UtcNow;
        var events = new List<EngineEvent>();

        // Event 1: VaultProfitDistributionInitiated
        events.Add(EngineEvent.Create("VaultProfitDistributionInitiated", vaultId,
            new Dictionary<string, object>
            {
                ["distributionId"] = distributionIdRaw,
                ["vaultId"] = vaultIdRaw,
                ["vaultAccountId"] = vaultAccountIdRaw,
                ["initiatorIdentityId"] = initiatorIdRaw,
                ["totalProfitAmount"] = totalProfitAmount.Value,
                ["currency"] = currency,
                ["participantCount"] = participantShares.Count,
                ["distributionReference"] = distributionReference,
                ["topic"] = "whyce.economic.events"
            }));

        // Event 2: VaultProfitDistributionCalculated
        var shareDetails = new List<Dictionary<string, object>>();
        foreach (var (recipientId, amount) in participantShares)
        {
            shareDetails.Add(new Dictionary<string, object>
            {
                ["recipientId"] = recipientId.ToString(),
                ["amount"] = amount
            });
        }

        events.Add(EngineEvent.Create("VaultProfitDistributionCalculated", vaultId,
            new Dictionary<string, object>
            {
                ["distributionId"] = distributionIdRaw,
                ["vaultId"] = vaultIdRaw,
                ["totalProfitAmount"] = totalProfitAmount.Value,
                ["participantCount"] = participantShares.Count,
                ["shares"] = shareDetails,
                ["topic"] = "whyce.economic.events"
            }));

        // Event 3: Debit ledger entry for vault account
        var debitTransactionId = Guid.NewGuid();
        events.Add(EngineEvent.Create("VaultLedgerEntryAppended", vaultId,
            new Dictionary<string, object>
            {
                ["transactionId"] = debitTransactionId.ToString(),
                ["distributionId"] = distributionIdRaw,
                ["vaultId"] = vaultIdRaw,
                ["vaultAccountId"] = vaultAccountIdRaw,
                ["amount"] = totalProfitAmount.Value,
                ["currency"] = currency,
                ["direction"] = "Debit",
                ["transactionType"] = "Distribution",
                ["topic"] = "whyce.economic.events"
            }));

        // Events 4..N: Credit ledger entries for each participant
        foreach (var (recipientId, amount) in participantShares)
        {
            var creditTransactionId = Guid.NewGuid();
            events.Add(EngineEvent.Create("VaultLedgerEntryAppended", vaultId,
                new Dictionary<string, object>
                {
                    ["transactionId"] = creditTransactionId.ToString(),
                    ["distributionId"] = distributionIdRaw,
                    ["vaultId"] = vaultIdRaw,
                    ["recipientId"] = recipientId.ToString(),
                    ["amount"] = amount,
                    ["currency"] = currency,
                    ["direction"] = "Credit",
                    ["transactionType"] = "Distribution",
                    ["topic"] = "whyce.economic.events"
                }));
        }

        // Event N+1: Transaction registered in registry
        events.Add(EngineEvent.Create("VaultTransactionRegistered", distributionId,
            new Dictionary<string, object>
            {
                ["distributionId"] = distributionIdRaw,
                ["vaultId"] = vaultIdRaw,
                ["vaultAccountId"] = vaultAccountIdRaw,
                ["initiatorIdentityId"] = initiatorIdRaw,
                ["transactionType"] = "Distribution",
                ["amount"] = totalProfitAmount.Value,
                ["currency"] = currency,
                ["participantCount"] = participantShares.Count,
                ["topic"] = "whyce.economic.events"
            }));

        // Event N+2: VaultProfitDistributionCompleted
        events.Add(EngineEvent.Create("VaultProfitDistributionCompleted", vaultId,
            new Dictionary<string, object>
            {
                ["distributionId"] = distributionIdRaw,
                ["vaultId"] = vaultIdRaw,
                ["totalProfitAmount"] = totalProfitAmount.Value,
                ["currency"] = currency,
                ["participantCount"] = participantShares.Count,
                ["distributionStatus"] = "Completed",
                ["completedAt"] = completedAt.ToString("O"),
                ["topic"] = "whyce.economic.events"
            }));

        var output = new Dictionary<string, object>
        {
            ["distributionId"] = distributionIdRaw,
            ["vaultId"] = vaultIdRaw,
            ["totalProfitAmount"] = totalProfitAmount.Value,
            ["participantCount"] = participantShares.Count,
            ["currency"] = currency,
            ["distributionStatus"] = "Completed",
            ["completedAt"] = completedAt.ToString("O")
        };

        return Task.FromResult(EngineResult.Ok(events, output));
    }

    private static List<AllocationEntry>? ResolveAllocations(IReadOnlyDictionary<string, object> data)
    {
        var allocationsRaw = data.GetValueOrDefault("allocations");
        if (allocationsRaw is IReadOnlyList<Dictionary<string, object>> typedList)
        {
            var result = new List<AllocationEntry>();
            foreach (var entry in typedList)
            {
                var recipientIdStr = entry.GetValueOrDefault("recipientId") as string;
                if (!Guid.TryParse(recipientIdStr, out var recipientId))
                    return null;

                var percentage = ResolveDecimalFromObject(entry.GetValueOrDefault("percentage"));
                if (percentage is null)
                    return null;

                var status = entry.GetValueOrDefault("status") as string ?? "Active";
                result.Add(new AllocationEntry(recipientId, percentage.Value, status));
            }
            return result;
        }

        if (allocationsRaw is List<Dictionary<string, object>> mutableList)
        {
            var result = new List<AllocationEntry>();
            foreach (var entry in mutableList)
            {
                var recipientIdStr = entry.GetValueOrDefault("recipientId") as string;
                if (!Guid.TryParse(recipientIdStr, out var recipientId))
                    return null;

                var percentage = ResolveDecimalFromObject(entry.GetValueOrDefault("percentage"));
                if (percentage is null)
                    return null;

                var status = entry.GetValueOrDefault("status") as string ?? "Active";
                result.Add(new AllocationEntry(recipientId, percentage.Value, status));
            }
            return result;
        }

        return null;
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

    private static decimal? ResolveDecimalFromObject(object? value) => ResolveDecimal(value);

    private sealed record AllocationEntry(Guid RecipientId, decimal Percentage, string Status);
}