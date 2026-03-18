namespace Whycespace.Systems.Midstream.WhycePlus.Forecasting;

public sealed class RevenueForecast
{
    private readonly List<RevenueForecastEntry> _forecasts = new();

    public RevenueForecastEntry ProjectRevenue(string clusterId, decimal currentRevenue, decimal projectedGrowthRate, int horizonMonths)
    {
        var projectedRevenue = currentRevenue * (decimal)Math.Pow((double)(1 + projectedGrowthRate), horizonMonths);
        var entry = new RevenueForecastEntry(
            Guid.NewGuid().ToString(), clusterId, currentRevenue,
            projectedRevenue, projectedGrowthRate, horizonMonths, DateTimeOffset.UtcNow);
        _forecasts.Add(entry);
        return entry;
    }

    public IReadOnlyList<RevenueForecastEntry> GetForecasts(string? clusterId = null)
    {
        return clusterId is null
            ? _forecasts
            : _forecasts.Where(f => f.ClusterId == clusterId).ToList();
    }
}

public sealed record RevenueForecastEntry(
    string ForecastId,
    string ClusterId,
    decimal CurrentRevenue,
    decimal ProjectedRevenue,
    decimal GrowthRate,
    int HorizonMonths,
    DateTimeOffset GeneratedAt
);
