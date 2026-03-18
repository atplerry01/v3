namespace Whycespace.Engines.T3I.Forecasting.Revenue.Models;

public sealed record RevenueForecastResult(
    bool Success,
    Guid SpvId,
    IReadOnlyList<RevenueForecastPeriod> ForecastedPeriods,
    decimal ProjectedTotalRevenue,
    decimal ProjectedUndistributed,
    RevenueGrowthTrend Trend,
    string? Error)
{
    public static RevenueForecastResult Ok(
        Guid spvId,
        IReadOnlyList<RevenueForecastPeriod> periods,
        decimal projectedTotalRevenue,
        decimal projectedUndistributed,
        RevenueGrowthTrend trend) =>
        new(true, spvId, periods, projectedTotalRevenue, projectedUndistributed, trend, null);

    public static RevenueForecastResult Fail(string error) =>
        new(false, Guid.Empty, [], 0m, 0m, RevenueGrowthTrend.Flat, error);
}

public sealed record RevenueForecastPeriod(
    int PeriodIndex,
    decimal ProjectedRevenue,
    decimal ProjectedDistributions,
    decimal ProjectedUndistributed,
    DateTimeOffset PeriodStart);

public enum RevenueGrowthTrend
{
    Declining,
    Flat,
    Growing,
    Accelerating
}
