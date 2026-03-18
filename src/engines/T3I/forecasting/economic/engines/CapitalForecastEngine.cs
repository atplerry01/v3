using Whycespace.Engines.T3I.Forecasting.Economic.Models;
using Whycespace.Engines.T3I.Shared;

namespace Whycespace.Engines.T3I.Forecasting.Economic.Engines;

public sealed class CapitalForecastEngine : IIntelligenceEngine<CapitalForecastInput, CapitalForecastResult>
{
    public string EngineName => "CapitalForecast";

    private const int MinimumSnapshotsForForecast = 2;

    public IntelligenceResult<CapitalForecastResult> Execute(IntelligenceContext<CapitalForecastInput> context)
    {
        var startedAt = DateTimeOffset.UtcNow;
        var input = context.Input;

        var validationError = Validate(input);
        if (validationError is not null)
        {
            return IntelligenceResult<CapitalForecastResult>.Fail(validationError,
                IntelligenceTrace.Create(EngineName, context.CorrelationId, startedAt));
        }

        var snapshots = input.HistoricalSnapshots
            .OrderBy(s => s.RecordedAt)
            .ToList();

        var avgContributionRate = ComputeAverageRate(snapshots, s => s.TotalContributions);
        var avgDistributionRate = ComputeAverageRate(snapshots, s => s.TotalDistributions);

        var lastSnapshot = snapshots[^1];
        var periods = new List<CapitalForecastPeriod>();
        var runningBalance = lastSnapshot.NetBalance;

        for (var i = 0; i < input.ForecastPeriods; i++)
        {
            var projectedContributions = avgContributionRate;
            var projectedDistributions = avgDistributionRate;
            runningBalance += projectedContributions - projectedDistributions;

            periods.Add(new CapitalForecastPeriod(
                PeriodIndex: i + 1,
                ProjectedContributions: projectedContributions,
                ProjectedDistributions: projectedDistributions,
                ProjectedNetBalance: runningBalance,
                PeriodStart: input.AsOf.AddMonths(i + 1)));
        }

        var confidence = DetermineConfidence(snapshots.Count);

        var result = CapitalForecastResult.Ok(
            input.SpvId,
            periods,
            runningBalance,
            confidence);

        return IntelligenceResult<CapitalForecastResult>.Ok(result,
            IntelligenceTrace.Create(EngineName, context.CorrelationId, startedAt));
    }

    private static decimal ComputeAverageRate(
        IReadOnlyList<CapitalBalanceSnapshot> snapshots,
        Func<CapitalBalanceSnapshot, decimal> selector)
    {
        if (snapshots.Count < MinimumSnapshotsForForecast)
            return 0m;

        var deltas = new List<decimal>();
        for (var i = 1; i < snapshots.Count; i++)
        {
            deltas.Add(selector(snapshots[i]) - selector(snapshots[i - 1]));
        }

        return deltas.Count > 0 ? deltas.Average() : 0m;
    }

    private static ForecastConfidence DetermineConfidence(int snapshotCount)
    {
        return snapshotCount switch
        {
            >= 12 => ForecastConfidence.High,
            >= 6 => ForecastConfidence.Medium,
            _ => ForecastConfidence.Low
        };
    }

    private static string? Validate(CapitalForecastInput input)
    {
        if (input.SpvId == Guid.Empty)
            return "SpvId must not be empty";

        if (input.HistoricalSnapshots.Count < MinimumSnapshotsForForecast)
            return $"At least {MinimumSnapshotsForForecast} historical snapshots are required";

        if (input.ForecastPeriods <= 0)
            return "ForecastPeriods must be greater than zero";

        if (input.ForecastPeriods > 60)
            return "ForecastPeriods must not exceed 60";

        return null;
    }
}
