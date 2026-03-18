namespace Whycespace.Engines.T3I.Forecasting.Economic.Models;

public sealed record CapitalForecastInput(
    Guid SpvId,
    IReadOnlyList<CapitalBalanceSnapshot> HistoricalSnapshots,
    int ForecastPeriods,
    DateTimeOffset AsOf);

public sealed record CapitalBalanceSnapshot(
    decimal NetBalance,
    decimal TotalContributions,
    decimal TotalDistributions,
    DateTimeOffset RecordedAt);
