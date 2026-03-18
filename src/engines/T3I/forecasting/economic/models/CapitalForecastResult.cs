namespace Whycespace.Engines.T3I.Forecasting.Economic.Models;

public sealed record CapitalForecastResult(
    bool Success,
    Guid SpvId,
    IReadOnlyList<CapitalForecastPeriod> ForecastedPeriods,
    decimal ProjectedNetBalance,
    ForecastConfidence Confidence,
    string? Error)
{
    public static CapitalForecastResult Ok(
        Guid spvId,
        IReadOnlyList<CapitalForecastPeriod> periods,
        decimal projectedNetBalance,
        ForecastConfidence confidence) =>
        new(true, spvId, periods, projectedNetBalance, confidence, null);

    public static CapitalForecastResult Fail(string error) =>
        new(false, Guid.Empty, [], 0m, ForecastConfidence.Low, error);
}

public sealed record CapitalForecastPeriod(
    int PeriodIndex,
    decimal ProjectedContributions,
    decimal ProjectedDistributions,
    decimal ProjectedNetBalance,
    DateTimeOffset PeriodStart);

public enum ForecastConfidence
{
    Low,
    Medium,
    High
}
