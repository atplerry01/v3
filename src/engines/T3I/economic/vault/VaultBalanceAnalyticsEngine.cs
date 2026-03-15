namespace Whycespace.Engines.T3I.Economic.Vault;

using global::System.Security.Cryptography;
using global::System.Text;

public sealed class VaultBalanceAnalyticsEngine
{
    public VaultBalanceAnalyticsResult ExecuteAnalytics(
        ExecuteVaultBalanceAnalyticsCommand command,
        IReadOnlyList<LedgerEntry> ledgerEntries)
    {
        var validationError = Validate(command);
        if (validationError is not null)
        {
            return new VaultBalanceAnalyticsResult(
                AnalyticsId: command.AnalyticsId,
                VaultId: command.VaultId,
                CurrentBalance: 0m,
                AverageBalance: 0m,
                MinimumBalance: 0m,
                MaximumBalance: 0m,
                BalanceGrowthRate: 0m,
                BalanceTrend: "Unknown",
                AnalyticsSummary: validationError,
                CompletedAt: DateTime.UtcNow);
        }

        var filtered = FilterLedgerEntries(ledgerEntries, command);

        var runningBalances = ComputeRunningBalances(filtered);

        var currentBalance = runningBalances.Count > 0 ? runningBalances[^1] : 0m;
        var averageBalance = runningBalances.Count > 0 ? runningBalances.Average() : 0m;
        var minimumBalance = runningBalances.Count > 0 ? runningBalances.Min() : 0m;
        var maximumBalance = runningBalances.Count > 0 ? runningBalances.Max() : 0m;
        var growthRate = ComputeGrowthRate(runningBalances);
        var trend = DetermineTrend(runningBalances);

        var summary = BuildAnalyticsSummary(
            command, filtered.Count, currentBalance, averageBalance,
            minimumBalance, maximumBalance, growthRate, trend);

        var hash = GenerateAnalyticsHash(
            command.AnalyticsId, command.VaultId,
            currentBalance, averageBalance, minimumBalance, maximumBalance, growthRate);

        return new VaultBalanceAnalyticsResult(
            AnalyticsId: command.AnalyticsId,
            VaultId: command.VaultId,
            CurrentBalance: currentBalance,
            AverageBalance: averageBalance,
            MinimumBalance: minimumBalance,
            MaximumBalance: maximumBalance,
            BalanceGrowthRate: growthRate,
            BalanceTrend: trend,
            AnalyticsSummary: summary,
            CompletedAt: DateTime.UtcNow,
            AnalyticsHash: hash);
    }

    private static IReadOnlyList<LedgerEntry> FilterLedgerEntries(
        IReadOnlyList<LedgerEntry> entries, ExecuteVaultBalanceAnalyticsCommand command)
    {
        return entries
            .Where(e => e.VaultId == command.VaultId
                && e.Timestamp >= command.AnalysisStartTimestamp
                && e.Timestamp <= command.AnalysisEndTimestamp)
            .OrderBy(e => e.Timestamp)
            .ToList();
    }

    private static List<decimal> ComputeRunningBalances(IReadOnlyList<LedgerEntry> entries)
    {
        var balances = new List<decimal>();
        var running = 0m;

        foreach (var entry in entries)
        {
            running += entry.EntryType == "Credit" ? entry.Amount : -entry.Amount;
            balances.Add(running);
        }

        return balances;
    }

    private static decimal ComputeGrowthRate(List<decimal> balances)
    {
        if (balances.Count < 2)
            return 0m;

        var first = balances[0];
        var last = balances[^1];

        if (first == 0m)
            return last > 0m ? 100m : 0m;

        return Math.Round((last - first) / Math.Abs(first) * 100m, 4);
    }

    private static string DetermineTrend(List<decimal> balances)
    {
        if (balances.Count < 2)
            return "Stable";

        var first = balances[0];
        var last = balances[^1];

        if (last > first)
            return "Increasing";

        if (last < first)
            return "Decreasing";

        return "Stable";
    }

    private static string BuildAnalyticsSummary(
        ExecuteVaultBalanceAnalyticsCommand command,
        int entryCount,
        decimal currentBalance, decimal averageBalance,
        decimal minimumBalance, decimal maximumBalance,
        decimal growthRate, string trend)
    {
        return $"{command.AnalysisScope} for vault {command.VaultId}: " +
               $"{entryCount} entries analyzed, " +
               $"current={currentBalance}, avg={averageBalance}, " +
               $"min={minimumBalance}, max={maximumBalance}, " +
               $"growth={growthRate}%, trend={trend}";
    }

    private static string GenerateAnalyticsHash(
        Guid analyticsId, Guid vaultId,
        decimal currentBalance, decimal averageBalance,
        decimal minimumBalance, decimal maximumBalance,
        decimal growthRate)
    {
        var input = $"{analyticsId}|{vaultId}|{currentBalance}|{averageBalance}|{minimumBalance}|{maximumBalance}|{growthRate}";
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexStringLower(bytes);
    }

    private static string? Validate(ExecuteVaultBalanceAnalyticsCommand command)
    {
        if (command.AnalyticsId == Guid.Empty)
            return "AnalyticsId must not be empty";

        if (command.VaultId == Guid.Empty)
            return "VaultId must not be empty";

        if (command.RequestedBy == Guid.Empty)
            return "RequestedBy must not be empty";

        if (command.AnalysisEndTimestamp <= command.AnalysisStartTimestamp)
            return "AnalysisEndTimestamp must be after AnalysisStartTimestamp";

        if (!Enum.IsDefined(command.AnalysisScope))
            return $"Invalid analysis scope: {command.AnalysisScope}";

        return null;
    }
}
