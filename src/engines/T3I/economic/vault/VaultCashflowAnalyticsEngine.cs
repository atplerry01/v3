namespace Whycespace.Engines.T3I.Economic.Vault;

using global::System.Security.Cryptography;
using global::System.Text;
using Whycespace.Contracts.Engines;
using Whycespace.Runtime.EngineManifest.Attributes;
using Whycespace.Runtime.EngineManifest.Models;

[EngineManifest("VaultCashflowAnalytics", EngineTier.T3I, EngineKind.Projection, "ExecuteVaultCashflowAnalyticsCommand", typeof(EngineEvent))]
public sealed class VaultCashflowAnalyticsEngine : IEngine
{
    public string Name => "VaultCashflowAnalytics";

    private static readonly string[] InflowTypes = { "Contribution", "TransferIn", "ProfitDistributionIn" };
    private static readonly string[] OutflowTypes = { "Withdrawal", "TransferOut", "ProfitDistributionOut" };

    public Task<EngineResult> ExecuteAsync(EngineContext context)
    {
        var command = ResolveCommand(context);
        if (command is null)
            return Task.FromResult(EngineResult.Fail("Invalid cashflow analytics command: missing required fields"));

        if (command.AnalysisEndTimestamp <= command.AnalysisStartTimestamp)
            return Task.FromResult(EngineResult.Fail("AnalysisEndTimestamp must be after AnalysisStartTimestamp"));

        if (!Enum.TryParse<CashflowAnalysisScope>(command.AnalysisScope, true, out _))
            return Task.FromResult(EngineResult.Fail($"Invalid AnalysisScope: {command.AnalysisScope}"));

        var ledgerEntries = ResolveLedgerEntries(context);
        var analytics = ComputeAnalytics(command, ledgerEntries);

        var events = new[]
        {
            EngineEvent.Create("VaultCashflowAnalyticsCompleted", command.VaultId,
                new Dictionary<string, object>
                {
                    ["analyticsId"] = analytics.AnalyticsId.ToString(),
                    ["vaultId"] = analytics.VaultId.ToString(),
                    ["totalInflows"] = analytics.TotalInflows,
                    ["totalOutflows"] = analytics.TotalOutflows,
                    ["netCashflow"] = analytics.NetCashflow,
                    ["cashflowTrend"] = analytics.CashflowTrend,
                    ["completedAt"] = analytics.CompletedAt.ToString("O"),
                    ["topic"] = "whyce.economic.events"
                })
        };

        var output = new Dictionary<string, object>
        {
            ["analyticsId"] = analytics.AnalyticsId.ToString(),
            ["vaultId"] = analytics.VaultId.ToString(),
            ["totalInflows"] = analytics.TotalInflows,
            ["totalOutflows"] = analytics.TotalOutflows,
            ["netCashflow"] = analytics.NetCashflow,
            ["contributionCount"] = analytics.ContributionCount,
            ["withdrawalCount"] = analytics.WithdrawalCount,
            ["transferCount"] = analytics.TransferCount,
            ["cashflowTrend"] = analytics.CashflowTrend,
            ["analyticsSummary"] = analytics.AnalyticsSummary,
            ["completedAt"] = analytics.CompletedAt.ToString("O"),
            ["analyticsHash"] = analytics.AnalyticsHash ?? ""
        };

        return Task.FromResult(EngineResult.Ok(events, output));
    }

    public static VaultCashflowAnalyticsResult ComputeAnalytics(
        ExecuteVaultCashflowAnalyticsCommand command,
        IReadOnlyList<LedgerEntry> ledgerEntries)
    {
        var scope = Enum.TryParse<CashflowAnalysisScope>(command.AnalysisScope, true, out var parsed)
            ? parsed
            : CashflowAnalysisScope.FullCashflow;

        var filtered = FilterByScope(ledgerEntries, scope);

        decimal totalInflows = 0m;
        decimal totalOutflows = 0m;
        int contributionCount = 0;
        int withdrawalCount = 0;
        int transferCount = 0;

        foreach (var entry in filtered)
        {
            var classification = ClassifyEntry(entry.EntryType);

            switch (classification)
            {
                case EntryClassification.Inflow:
                    totalInflows += entry.Amount;
                    break;
                case EntryClassification.Outflow:
                    totalOutflows += entry.Amount;
                    break;
            }

            switch (entry.EntryType)
            {
                case "Contribution":
                    contributionCount++;
                    break;
                case "Withdrawal":
                    withdrawalCount++;
                    break;
                case "TransferIn":
                case "TransferOut":
                    transferCount++;
                    break;
            }
        }

        var netCashflow = totalInflows - totalOutflows;
        var trend = DetermineTrend(totalInflows, totalOutflows);
        var summary = GenerateSummary(totalInflows, totalOutflows, netCashflow, trend, filtered.Count);
        var hash = GenerateAnalyticsHash(command.AnalyticsId, command.VaultId, totalInflows, totalOutflows, netCashflow);

        return new VaultCashflowAnalyticsResult(
            command.AnalyticsId,
            command.VaultId,
            totalInflows,
            totalOutflows,
            netCashflow,
            contributionCount,
            withdrawalCount,
            transferCount,
            trend,
            summary,
            DateTime.UtcNow,
            hash);
    }

    public static EntryClassification ClassifyEntry(string entryType)
    {
        if (InflowTypes.Contains(entryType, StringComparer.OrdinalIgnoreCase))
            return EntryClassification.Inflow;

        if (OutflowTypes.Contains(entryType, StringComparer.OrdinalIgnoreCase))
            return EntryClassification.Outflow;

        return EntryClassification.Unknown;
    }

    public static string DetermineTrend(decimal totalInflows, decimal totalOutflows)
    {
        if (totalInflows == 0m && totalOutflows == 0m)
            return "Neutral";

        var ratio = totalOutflows > 0m ? totalInflows / totalOutflows : decimal.MaxValue;

        if (ratio > 1.05m)
            return "Positive";
        if (ratio < 0.95m)
            return "Negative";
        return "Neutral";
    }

    private static IReadOnlyList<LedgerEntry> FilterByScope(
        IReadOnlyList<LedgerEntry> entries,
        CashflowAnalysisScope scope)
    {
        return scope switch
        {
            CashflowAnalysisScope.ContributionFlow => entries
                .Where(e => e.EntryType is "Contribution")
                .ToList(),
            CashflowAnalysisScope.WithdrawalFlow => entries
                .Where(e => e.EntryType is "Withdrawal")
                .ToList(),
            CashflowAnalysisScope.TransferFlow => entries
                .Where(e => e.EntryType is "TransferIn" or "TransferOut")
                .ToList(),
            CashflowAnalysisScope.FullCashflow => entries.ToList(),
            _ => entries.ToList()
        };
    }

    private static string GenerateSummary(
        decimal totalInflows,
        decimal totalOutflows,
        decimal netCashflow,
        string trend,
        int entryCount)
    {
        return $"Analyzed {entryCount} ledger entries. " +
               $"Total inflows: {totalInflows:N2}, total outflows: {totalOutflows:N2}, " +
               $"net cashflow: {netCashflow:N2}. Trend: {trend}.";
    }

    private static string GenerateAnalyticsHash(
        Guid analyticsId,
        Guid vaultId,
        decimal totalInflows,
        decimal totalOutflows,
        decimal netCashflow)
    {
        var input = $"{analyticsId}|{vaultId}|{totalInflows}|{totalOutflows}|{netCashflow}";
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexStringLower(bytes);
    }

    private static ExecuteVaultCashflowAnalyticsCommand? ResolveCommand(EngineContext context)
    {
        var analyticsId = ResolveGuid(context.Data.GetValueOrDefault("analyticsId"));
        var vaultId = ResolveGuid(context.Data.GetValueOrDefault("vaultId"));
        var requestedBy = ResolveGuid(context.Data.GetValueOrDefault("requestedBy"));
        var analysisScope = context.Data.GetValueOrDefault("analysisScope") as string;
        var startTimestamp = ResolveDateTime(context.Data.GetValueOrDefault("analysisStartTimestamp"));
        var endTimestamp = ResolveDateTime(context.Data.GetValueOrDefault("analysisEndTimestamp"));

        if (analyticsId is null || vaultId is null || requestedBy is null)
            return null;
        if (string.IsNullOrEmpty(analysisScope))
            return null;
        if (startTimestamp is null || endTimestamp is null)
            return null;

        var referenceId = context.Data.GetValueOrDefault("referenceId") as string;
        var referenceType = context.Data.GetValueOrDefault("referenceType") as string;

        return new ExecuteVaultCashflowAnalyticsCommand(
            analyticsId.Value,
            vaultId.Value,
            startTimestamp.Value,
            endTimestamp.Value,
            analysisScope,
            requestedBy.Value,
            referenceId,
            referenceType);
    }

    private static IReadOnlyList<LedgerEntry> ResolveLedgerEntries(EngineContext context)
    {
        var entries = new List<LedgerEntry>();
        var ledgerData = context.Data.GetValueOrDefault("ledgerEntries");

        if (ledgerData is IReadOnlyList<LedgerEntry> typed)
            return typed;

        if (ledgerData is IEnumerable<object> items)
        {
            foreach (var item in items)
            {
                if (item is IReadOnlyDictionary<string, object> roDict)
                {
                    var entryType = roDict.GetValueOrDefault("entryType") as string;
                    var amount = ResolveDecimal(roDict.GetValueOrDefault("amount"));
                    var timestamp = ResolveDateTime(roDict.GetValueOrDefault("timestamp"));

                    if (entryType is not null && amount is not null && timestamp is not null)
                    {
                        var entryId = ResolveGuid(roDict.GetValueOrDefault("entryId")) ?? Guid.NewGuid();
                        var entryVaultId = ResolveGuid(roDict.GetValueOrDefault("vaultId")) ?? Guid.Empty;
                        entries.Add(new LedgerEntry(entryId, entryVaultId, entryType, amount.Value, timestamp.Value));
                    }
                }
                else if (item is IDictionary<string, object> dict)
                {
                    dict.TryGetValue("entryType", out var etVal);
                    dict.TryGetValue("amount", out var amtVal);
                    dict.TryGetValue("timestamp", out var tsVal);

                    var entryType = etVal as string;
                    var amount = ResolveDecimal(amtVal);
                    var timestamp = ResolveDateTime(tsVal);

                    if (entryType is not null && amount is not null && timestamp is not null)
                    {
                        dict.TryGetValue("entryId", out var eidVal);
                        dict.TryGetValue("vaultId", out var vidVal);
                        var entryId = ResolveGuid(eidVal) ?? Guid.NewGuid();
                        var entryVaultId = ResolveGuid(vidVal) ?? Guid.Empty;
                        entries.Add(new LedgerEntry(entryId, entryVaultId, entryType, amount.Value, timestamp.Value));
                    }
                }
            }
        }

        return entries;
    }

    private static Guid? ResolveGuid(object? value)
    {
        return value switch
        {
            Guid g => g,
            string s when Guid.TryParse(s, out var parsed) => parsed,
            _ => null
        };
    }

    private static DateTime? ResolveDateTime(object? value)
    {
        return value switch
        {
            DateTime dt => dt,
            DateTimeOffset dto => dto.UtcDateTime,
            string s when DateTime.TryParse(s, out var parsed) => parsed,
            _ => null
        };
    }

    private static decimal? ResolveDecimal(object? value)
    {
        return value switch
        {
            decimal d => d,
            double dbl => (decimal)dbl,
            int i => i,
            long l => l,
            string s when decimal.TryParse(s, out var parsed) => parsed,
            _ => null
        };
    }
}

public enum EntryClassification
{
    Inflow,
    Outflow,
    Unknown
}
