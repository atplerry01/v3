namespace Whycespace.Systems.Midstream.WhycePlus.Forecasting;

public sealed class DemandForecast
{
    private readonly List<ForecastEntry> _entries = new();

    public ForecastEntry GenerateForecast(string clusterId, string serviceType, decimal historicalAverage, decimal growthRate)
    {
        var projected = historicalAverage * (1 + growthRate);
        var entry = new ForecastEntry(
            Guid.NewGuid().ToString(), clusterId, serviceType,
            historicalAverage, projected, growthRate, DateTimeOffset.UtcNow);
        _entries.Add(entry);
        return entry;
    }

    public IReadOnlyList<ForecastEntry> GetForecasts(string? clusterId = null)
    {
        return clusterId is null
            ? _entries
            : _entries.Where(e => e.ClusterId == clusterId).ToList();
    }
}

public sealed record ForecastEntry(
    string ForecastId,
    string ClusterId,
    string ServiceType,
    decimal HistoricalAverage,
    decimal ProjectedDemand,
    decimal GrowthRate,
    DateTimeOffset GeneratedAt
);
