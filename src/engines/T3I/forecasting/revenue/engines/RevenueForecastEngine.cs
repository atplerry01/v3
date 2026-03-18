using Whycespace.Engines.T3I.Forecasting.Revenue.Models;
using Whycespace.Engines.T3I.Shared;

namespace Whycespace.Engines.T3I.Forecasting.Revenue.Engines;

public sealed class RevenueForecastEngine : IIntelligenceEngine<RevenueForecastInput, RevenueForecastResult>
{
    public string EngineName => "RevenueForecast";

    private const int MinimumSnapshotsForForecast = 2;

    public IntelligenceResult<RevenueForecastResult> Execute(IntelligenceContext<RevenueForecastInput> context)
    {
        var startedAt = DateTimeOffset.UtcNow;
        var input = context.Input;

        var validationError = Validate(input);
        if (validationError is not null)
        {
            return IntelligenceResult<RevenueForecastResult>.Fail(validationError,
                IntelligenceTrace.Create(EngineName, context.CorrelationId, startedAt));
        }

        var snapshots = input.HistoricalSnapshots
            .OrderBy(s => s.RecordedAt)
            .ToList();

        var revenueDeltas = ComputeDeltas(snapshots, s => s.TotalRevenue);
        var distributionDeltas = ComputeDeltas(snapshots, s => s.TotalProfitDistributed);
        var avgRevenueGrowth = revenueDeltas.Count > 0 ? revenueDeltas.Average() : 0m;
        var avgDistributionGrowth = distributionDeltas.Count > 0 ? distributionDeltas.Average() : 0m;

        var lastSnapshot = snapshots[^1];
        var runningRevenue = lastSnapshot.TotalRevenue;
        var runningDistributed = lastSnapshot.TotalProfitDistributed;
        var periods = new List<RevenueForecastPeriod>();

        for (var i = 0; i < input.ForecastPeriods; i++)
        {
            runningRevenue += avgRevenueGrowth;
            runningDistributed += avgDistributionGrowth;
            var undistributed = runningRevenue - runningDistributed;

            periods.Add(new RevenueForecastPeriod(
                PeriodIndex: i + 1,
                ProjectedRevenue: avgRevenueGrowth,
                ProjectedDistributions: avgDistributionGrowth,
                ProjectedUndistributed: undistributed,
                PeriodStart: input.AsOf.AddMonths(i + 1)));
        }

        var trend = ClassifyTrend(revenueDeltas);

        var result = RevenueForecastResult.Ok(
            input.SpvId,
            periods,
            runningRevenue,
            runningRevenue - runningDistributed,
            trend);

        return IntelligenceResult<RevenueForecastResult>.Ok(result,
            IntelligenceTrace.Create(EngineName, context.CorrelationId, startedAt));
    }

    private static List<decimal> ComputeDeltas(
        IReadOnlyList<RevenueSnapshot> snapshots,
        Func<RevenueSnapshot, decimal> selector)
    {
        var deltas = new List<decimal>();
        for (var i = 1; i < snapshots.Count; i++)
        {
            deltas.Add(selector(snapshots[i]) - selector(snapshots[i - 1]));
        }
        return deltas;
    }

    private static RevenueGrowthTrend ClassifyTrend(IReadOnlyList<decimal> deltas)
    {
        if (deltas.Count < 2)
            return RevenueGrowthTrend.Flat;

        var avgFirst = deltas.Take(deltas.Count / 2).Average();
        var avgSecond = deltas.Skip(deltas.Count / 2).Average();

        if (avgSecond > avgFirst * 1.1m)
            return RevenueGrowthTrend.Accelerating;

        if (avgSecond > 0m)
            return RevenueGrowthTrend.Growing;

        if (avgSecond < -0.01m)
            return RevenueGrowthTrend.Declining;

        return RevenueGrowthTrend.Flat;
    }

    private static string? Validate(RevenueForecastInput input)
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
