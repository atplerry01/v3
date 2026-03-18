namespace Whycespace.Engines.T3I.Forecasting.Revenue.Models;

public sealed record RevenueForecastInput(
    Guid SpvId,
    IReadOnlyList<RevenueSnapshot> HistoricalSnapshots,
    int ForecastPeriods,
    DateTimeOffset AsOf);

public sealed record RevenueSnapshot(
    decimal TotalRevenue,
    decimal TotalProfitDistributed,
    decimal UndistributedRevenue,
    int RevenueEventCount,
    DateTimeOffset RecordedAt);
