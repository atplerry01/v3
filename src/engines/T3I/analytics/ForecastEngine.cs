namespace Whycespace.Engines.T3I_Intelligence;

using Whycespace.Contracts.Engines;
using Whycespace.EngineManifest.Manifest;
using Whycespace.EngineManifest.Models;

[EngineManifest("Forecast", EngineTier.T3I, EngineKind.Decision, "ForecastRequest", typeof(EngineEvent))]
public sealed class ForecastEngine : IEngine
{
    public string Name => "Forecast";

    public Task<EngineResult> ExecuteAsync(EngineContext context)
    {
        var forecastType = context.Data.GetValueOrDefault("forecastType") as string;
        if (string.IsNullOrEmpty(forecastType))
            return Task.FromResult(EngineResult.Fail("Missing forecastType"));

        return forecastType switch
        {
            "TaxiDemand" => ForecastTaxiDemand(context),
            "PropertyDemand" => ForecastPropertyDemand(context),
            "RevenueForecast" => ForecastRevenue(context),
            "CapacityPlanning" => ForecastCapacity(context),
            _ => Task.FromResult(EngineResult.Fail($"Unknown forecastType: {forecastType}"))
        };
    }

    private static Task<EngineResult> ForecastTaxiDemand(EngineContext context)
    {
        var region = context.Data.GetValueOrDefault("region") as string ?? "default";
        var currentDemand = ResolveInt(context.Data.GetValueOrDefault("currentDemand")) ?? 0;
        var historicalAvg = ResolveInt(context.Data.GetValueOrDefault("historicalAvg")) ?? 0;
        var dayOfWeek = ResolveInt(context.Data.GetValueOrDefault("dayOfWeek"))
            ?? (int)DateTimeOffset.UtcNow.DayOfWeek;
        var hourOfDay = ResolveInt(context.Data.GetValueOrDefault("hourOfDay"))
            ?? DateTimeOffset.UtcNow.Hour;

        var timeMultiplier = ComputeTimeMultiplier(dayOfWeek, hourOfDay);
        var trendFactor = historicalAvg > 0
            ? (double)currentDemand / historicalAvg
            : 1.0;

        var baseForecast = historicalAvg > 0 ? historicalAvg : currentDemand;
        var predictedDemand = (int)Math.Round(baseForecast * timeMultiplier * trendFactor);
        var recommendedDrivers = (int)Math.Ceiling(predictedDemand * 1.2);

        var confidence = ComputeConfidence(historicalAvg, currentDemand);
        var surgeIndicator = predictedDemand > currentDemand * 1.5 ? "high" :
                             predictedDemand > currentDemand * 1.2 ? "moderate" : "normal";

        var events = new[]
        {
            EngineEvent.Create("TaxiDemandForecast", Guid.Parse(context.WorkflowId),
                new Dictionary<string, object>
                {
                    ["region"] = region,
                    ["currentDemand"] = currentDemand,
                    ["predictedDemand"] = predictedDemand,
                    ["recommendedDrivers"] = recommendedDrivers,
                    ["timeMultiplier"] = timeMultiplier,
                    ["trendFactor"] = trendFactor,
                    ["confidence"] = confidence,
                    ["surgeIndicator"] = surgeIndicator,
                    ["dayOfWeek"] = dayOfWeek,
                    ["hourOfDay"] = hourOfDay,
                    ["topic"] = "whyce.cluster.events"
                })
        };

        return Task.FromResult(EngineResult.Ok(events,
            new Dictionary<string, object>
            {
                ["forecastType"] = "TaxiDemand",
                ["region"] = region,
                ["predictedDemand"] = predictedDemand,
                ["recommendedDrivers"] = recommendedDrivers,
                ["confidence"] = confidence,
                ["surgeIndicator"] = surgeIndicator
            }));
    }

    private static Task<EngineResult> ForecastPropertyDemand(EngineContext context)
    {
        var area = context.Data.GetValueOrDefault("area") as string ?? "default";
        var currentListings = ResolveInt(context.Data.GetValueOrDefault("currentListings")) ?? 0;
        var currentApplications = ResolveInt(context.Data.GetValueOrDefault("currentApplications")) ?? 0;
        var historicalOccupancy = ResolveDouble(context.Data.GetValueOrDefault("historicalOccupancy")) ?? 0.85;
        var avgMonthlyRent = ResolveDecimal(context.Data.GetValueOrDefault("avgMonthlyRent")) ?? 0m;

        var demandRatio = currentListings > 0
            ? (double)currentApplications / currentListings
            : 0.0;

        var marketPressure = demandRatio switch
        {
            > 3.0 => "very-high",
            > 2.0 => "high",
            > 1.0 => "balanced",
            > 0.5 => "low",
            _ => "oversupply"
        };

        var predictedOccupancy = Math.Min(1.0, historicalOccupancy * (1.0 + (demandRatio - 1.0) * 0.1));
        var predictedVacancies = (int)Math.Round(currentListings * (1.0 - predictedOccupancy));

        var rentAdjustment = demandRatio switch
        {
            > 2.0 => 1.05m,
            > 1.5 => 1.02m,
            > 1.0 => 1.0m,
            > 0.5 => 0.98m,
            _ => 0.95m
        };

        var suggestedRent = Math.Round(avgMonthlyRent * rentAdjustment, 2);
        var confidence = ComputeConfidence(currentListings, currentApplications);

        var events = new[]
        {
            EngineEvent.Create("PropertyDemandForecast", Guid.Parse(context.WorkflowId),
                new Dictionary<string, object>
                {
                    ["area"] = area,
                    ["currentListings"] = currentListings,
                    ["currentApplications"] = currentApplications,
                    ["demandRatio"] = Math.Round(demandRatio, 3),
                    ["marketPressure"] = marketPressure,
                    ["predictedOccupancy"] = Math.Round(predictedOccupancy, 3),
                    ["predictedVacancies"] = predictedVacancies,
                    ["avgMonthlyRent"] = avgMonthlyRent,
                    ["suggestedRent"] = suggestedRent,
                    ["rentAdjustment"] = rentAdjustment,
                    ["confidence"] = confidence,
                    ["topic"] = "whyce.cluster.events"
                })
        };

        return Task.FromResult(EngineResult.Ok(events,
            new Dictionary<string, object>
            {
                ["forecastType"] = "PropertyDemand",
                ["area"] = area,
                ["marketPressure"] = marketPressure,
                ["predictedOccupancy"] = Math.Round(predictedOccupancy, 3),
                ["predictedVacancies"] = predictedVacancies,
                ["suggestedRent"] = suggestedRent,
                ["confidence"] = confidence
            }));
    }

    private static Task<EngineResult> ForecastRevenue(EngineContext context)
    {
        var spvId = context.Data.GetValueOrDefault("spvId") as string;
        if (string.IsNullOrEmpty(spvId))
            return Task.FromResult(EngineResult.Fail("Missing spvId"));

        var currentMonthRevenue = ResolveDecimal(context.Data.GetValueOrDefault("currentMonthRevenue")) ?? 0m;
        var previousMonthRevenue = ResolveDecimal(context.Data.GetValueOrDefault("previousMonthRevenue")) ?? 0m;
        var monthsOfData = ResolveInt(context.Data.GetValueOrDefault("monthsOfData")) ?? 1;

        var growthRate = previousMonthRevenue > 0
            ? (currentMonthRevenue - previousMonthRevenue) / previousMonthRevenue
            : 0m;

        var dampedGrowth = growthRate * 0.7m;
        var nextMonthForecast = Math.Round(currentMonthRevenue * (1 + dampedGrowth), 2);
        var quarterForecast = Math.Round(nextMonthForecast * 3 * (1 + dampedGrowth), 2);
        var confidence = Math.Min(0.95, 0.5 + monthsOfData * 0.05);

        var events = new[]
        {
            EngineEvent.Create("RevenueForecast", Guid.Parse(context.WorkflowId),
                new Dictionary<string, object>
                {
                    ["spvId"] = spvId,
                    ["currentMonthRevenue"] = currentMonthRevenue,
                    ["growthRate"] = Math.Round(growthRate, 4),
                    ["nextMonthForecast"] = nextMonthForecast,
                    ["quarterForecast"] = quarterForecast,
                    ["confidence"] = confidence,
                    ["topic"] = "whyce.economic.events"
                })
        };

        return Task.FromResult(EngineResult.Ok(events,
            new Dictionary<string, object>
            {
                ["forecastType"] = "RevenueForecast",
                ["spvId"] = spvId,
                ["nextMonthForecast"] = nextMonthForecast,
                ["quarterForecast"] = quarterForecast,
                ["growthRate"] = Math.Round(growthRate, 4),
                ["confidence"] = confidence
            }));
    }

    private static Task<EngineResult> ForecastCapacity(EngineContext context)
    {
        var clusterId = context.Data.GetValueOrDefault("clusterId") as string;
        if (string.IsNullOrEmpty(clusterId))
            return Task.FromResult(EngineResult.Fail("Missing clusterId"));

        var currentCapacity = ResolveInt(context.Data.GetValueOrDefault("currentCapacity")) ?? 0;
        var currentUtilization = ResolveDouble(context.Data.GetValueOrDefault("currentUtilization")) ?? 0.0;
        var growthRatePercent = ResolveDouble(context.Data.GetValueOrDefault("growthRatePercent")) ?? 5.0;

        var projectedUtilization30d = Math.Min(1.0, currentUtilization * (1 + growthRatePercent / 100.0));
        var projectedUtilization90d = Math.Min(1.0, currentUtilization * Math.Pow(1 + growthRatePercent / 100.0, 3));

        var capacityNeeded30d = currentCapacity > 0
            ? (int)Math.Ceiling(currentCapacity * projectedUtilization30d / 0.8)
            : 0;
        var capacityNeeded90d = currentCapacity > 0
            ? (int)Math.Ceiling(currentCapacity * projectedUtilization90d / 0.8)
            : 0;

        var scaleAction = projectedUtilization30d switch
        {
            > 0.9 => "scale-up-urgent",
            > 0.8 => "scale-up-planned",
            > 0.5 => "maintain",
            _ => "scale-down"
        };

        var events = new[]
        {
            EngineEvent.Create("CapacityForecast", Guid.Parse(context.WorkflowId),
                new Dictionary<string, object>
                {
                    ["clusterId"] = clusterId,
                    ["currentCapacity"] = currentCapacity,
                    ["currentUtilization"] = Math.Round(currentUtilization, 3),
                    ["projectedUtilization30d"] = Math.Round(projectedUtilization30d, 3),
                    ["projectedUtilization90d"] = Math.Round(projectedUtilization90d, 3),
                    ["capacityNeeded30d"] = capacityNeeded30d,
                    ["capacityNeeded90d"] = capacityNeeded90d,
                    ["scaleAction"] = scaleAction,
                    ["topic"] = "whyce.cluster.events"
                })
        };

        return Task.FromResult(EngineResult.Ok(events,
            new Dictionary<string, object>
            {
                ["forecastType"] = "CapacityPlanning",
                ["clusterId"] = clusterId,
                ["projectedUtilization30d"] = Math.Round(projectedUtilization30d, 3),
                ["capacityNeeded30d"] = capacityNeeded30d,
                ["capacityNeeded90d"] = capacityNeeded90d,
                ["scaleAction"] = scaleAction
            }));
    }

    private static double ComputeTimeMultiplier(int dayOfWeek, int hourOfDay)
    {
        var dayFactor = dayOfWeek switch
        {
            0 or 6 => 0.8,
            4 or 5 => 1.2,
            _ => 1.0
        };

        var hourFactor = hourOfDay switch
        {
            >= 7 and <= 9 => 1.5,
            >= 17 and <= 19 => 1.6,
            >= 22 or <= 5 => 1.3,
            >= 12 and <= 14 => 1.1,
            _ => 1.0
        };

        return dayFactor * hourFactor;
    }

    private static double ComputeConfidence(int dataPoint1, int dataPoint2)
    {
        var dataVolume = dataPoint1 + dataPoint2;
        return dataVolume switch
        {
            > 1000 => 0.92,
            > 500 => 0.85,
            > 100 => 0.75,
            > 10 => 0.60,
            _ => 0.40
        };
    }

    private static int? ResolveInt(object? value)
    {
        return value switch
        {
            int i => i,
            long l => (int)l,
            double d => (int)d,
            decimal m => (int)m,
            string s when int.TryParse(s, out var parsed) => parsed,
            _ => null
        };
    }

    private static double? ResolveDouble(object? value)
    {
        return value switch
        {
            double d => d,
            decimal m => (double)m,
            int i => i,
            long l => l,
            string s when double.TryParse(s, out var parsed) => parsed,
            _ => null
        };
    }

    private static decimal? ResolveDecimal(object? value)
    {
        return value switch
        {
            decimal d => d,
            double d => (decimal)d,
            int i => i,
            long l => l,
            string s when decimal.TryParse(s, out var parsed) => parsed,
            _ => null
        };
    }
}
