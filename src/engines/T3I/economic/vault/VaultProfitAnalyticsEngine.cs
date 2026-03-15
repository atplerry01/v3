namespace Whycespace.Engines.T3I.Economic.Vault;

using global::System.Security.Cryptography;
using global::System.Text;
using Whycespace.Contracts.Engines;
using Whycespace.Runtime.EngineManifest.Attributes;
using Whycespace.Runtime.EngineManifest.Models;

[EngineManifest("VaultProfitAnalytics", EngineTier.T3I, EngineKind.Projection, "ExecuteVaultProfitAnalyticsCommand", typeof(EngineEvent))]
public sealed class VaultProfitAnalyticsEngine : IEngine
{
    public string Name => "VaultProfitAnalytics";

    private static readonly string[] ProfitGenerationTypes = { "RevenueAllocation", "InvestmentReturn", "ExternalYield" };
    private static readonly string[] ProfitDistributionTypes = { "ParticipantProfitDistribution", "OperatorProfitDistribution", "GovernanceDistribution" };

    public Task<EngineResult> ExecuteAsync(EngineContext context)
    {
        var command = ResolveCommand(context);
        if (command is null)
            return Task.FromResult(EngineResult.Fail("Invalid profit analytics command: missing required fields"));

        if (command.AnalysisEndTimestamp <= command.AnalysisStartTimestamp)
            return Task.FromResult(EngineResult.Fail("AnalysisEndTimestamp must be after AnalysisStartTimestamp"));

        if (!Enum.TryParse<ProfitAnalysisScope>(command.AnalysisScope, true, out _))
            return Task.FromResult(EngineResult.Fail($"Invalid AnalysisScope: {command.AnalysisScope}"));

        var ledgerEntries = ResolveLedgerEntries(context);
        var analytics = ComputeAnalytics(command, ledgerEntries);

        var events = new[]
        {
            EngineEvent.Create("VaultProfitAnalyticsCompleted", command.VaultId,
                new Dictionary<string, object>
                {
                    ["analyticsId"] = analytics.AnalyticsId.ToString(),
                    ["vaultId"] = analytics.VaultId.ToString(),
                    ["totalProfitGenerated"] = analytics.TotalProfitGenerated,
                    ["totalProfitDistributed"] = analytics.TotalProfitDistributed,
                    ["retainedProfit"] = analytics.RetainedProfit,
                    ["profitTrend"] = analytics.ProfitTrend,
                    ["completedAt"] = analytics.CompletedAt.ToString("O"),
                    ["topic"] = "whyce.economic.events"
                })
        };

        var output = new Dictionary<string, object>
        {
            ["analyticsId"] = analytics.AnalyticsId.ToString(),
            ["vaultId"] = analytics.VaultId.ToString(),
            ["totalProfitGenerated"] = analytics.TotalProfitGenerated,
            ["totalProfitDistributed"] = analytics.TotalProfitDistributed,
            ["retainedProfit"] = analytics.RetainedProfit,
            ["averageProfitPerDistribution"] = analytics.AverageProfitPerDistribution,
            ["profitDistributionCount"] = analytics.ProfitDistributionCount,
            ["profitTrend"] = analytics.ProfitTrend,
            ["analyticsSummary"] = analytics.AnalyticsSummary,
            ["completedAt"] = analytics.CompletedAt.ToString("O"),
            ["analyticsHash"] = analytics.AnalyticsHash ?? ""
        };

        return Task.FromResult(EngineResult.Ok(events, output));
    }

    public static VaultProfitAnalyticsResult ComputeAnalytics(
        ExecuteVaultProfitAnalyticsCommand command,
        IReadOnlyList<LedgerEntry> ledgerEntries)
    {
        var scope = Enum.TryParse<ProfitAnalysisScope>(command.AnalysisScope, true, out var parsed)
            ? parsed
            : ProfitAnalysisScope.FullProfitAnalysis;

        var filtered = FilterByScope(ledgerEntries, scope);

        decimal totalProfitGenerated = 0m;
        decimal totalProfitDistributed = 0m;
        int profitDistributionCount = 0;

        foreach (var entry in filtered)
        {
            var classification = ClassifyEntry(entry.EntryType);

            switch (classification)
            {
                case ProfitClassification.Generation:
                    totalProfitGenerated += entry.Amount;
                    break;
                case ProfitClassification.Distribution:
                    totalProfitDistributed += entry.Amount;
                    profitDistributionCount++;
                    break;
            }
        }

        var retainedProfit = totalProfitGenerated - totalProfitDistributed;
        var averageProfitPerDistribution = profitDistributionCount > 0
            ? Math.Round(totalProfitDistributed / profitDistributionCount, 2)
            : 0m;
        var trend = DetermineTrend(totalProfitGenerated, totalProfitDistributed);
        var summary = GenerateSummary(totalProfitGenerated, totalProfitDistributed, retainedProfit, trend, filtered.Count);
        var hash = GenerateAnalyticsHash(command.AnalyticsId, command.VaultId, totalProfitGenerated, totalProfitDistributed, retainedProfit);

        return new VaultProfitAnalyticsResult(
            command.AnalyticsId,
            command.VaultId,
            totalProfitGenerated,
            totalProfitDistributed,
            retainedProfit,
            averageProfitPerDistribution,
            profitDistributionCount,
            trend,
            summary,
            DateTime.UtcNow,
            hash);
    }

    public static ProfitClassification ClassifyEntry(string entryType)
    {
        if (ProfitGenerationTypes.Contains(entryType, StringComparer.OrdinalIgnoreCase))
            return ProfitClassification.Generation;

        if (ProfitDistributionTypes.Contains(entryType, StringComparer.OrdinalIgnoreCase))
            return ProfitClassification.Distribution;

        return ProfitClassification.Unknown;
    }

    public static string DetermineTrend(decimal totalProfitGenerated, decimal totalProfitDistributed)
    {
        if (totalProfitGenerated == 0m && totalProfitDistributed == 0m)
            return "Neutral";

        var retainedRatio = totalProfitGenerated > 0m
            ? (totalProfitGenerated - totalProfitDistributed) / totalProfitGenerated
            : 0m;

        if (retainedRatio > 0.30m)
            return "Increasing";
        if (retainedRatio < 0.05m)
            return "Declining";
        return "Stable";
    }

    private static IReadOnlyList<LedgerEntry> FilterByScope(
        IReadOnlyList<LedgerEntry> entries,
        ProfitAnalysisScope scope)
    {
        return scope switch
        {
            ProfitAnalysisScope.ProfitGeneration => entries
                .Where(e => ProfitGenerationTypes.Contains(e.EntryType, StringComparer.OrdinalIgnoreCase))
                .ToList(),
            ProfitAnalysisScope.ProfitDistribution => entries
                .Where(e => ProfitDistributionTypes.Contains(e.EntryType, StringComparer.OrdinalIgnoreCase))
                .ToList(),
            ProfitAnalysisScope.ParticipantProfitExposure => entries
                .Where(e => e.EntryType is "ParticipantProfitDistribution")
                .ToList(),
            ProfitAnalysisScope.FullProfitAnalysis => entries
                .Where(e => ProfitGenerationTypes.Contains(e.EntryType, StringComparer.OrdinalIgnoreCase)
                    || ProfitDistributionTypes.Contains(e.EntryType, StringComparer.OrdinalIgnoreCase))
                .ToList(),
            _ => entries.ToList()
        };
    }

    private static string GenerateSummary(
        decimal totalProfitGenerated,
        decimal totalProfitDistributed,
        decimal retainedProfit,
        string trend,
        int entryCount)
    {
        return $"Analyzed {entryCount} profit-related ledger entries. " +
               $"Total profit generated: {totalProfitGenerated:N2}, total distributed: {totalProfitDistributed:N2}, " +
               $"retained profit: {retainedProfit:N2}. Trend: {trend}.";
    }

    private static string GenerateAnalyticsHash(
        Guid analyticsId,
        Guid vaultId,
        decimal totalProfitGenerated,
        decimal totalProfitDistributed,
        decimal retainedProfit)
    {
        var input = $"{analyticsId}|{vaultId}|{totalProfitGenerated}|{totalProfitDistributed}|{retainedProfit}";
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexStringLower(bytes);
    }

    private static ExecuteVaultProfitAnalyticsCommand? ResolveCommand(EngineContext context)
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

        return new ExecuteVaultProfitAnalyticsCommand(
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

public enum ProfitClassification
{
    Generation,
    Distribution,
    Unknown
}
