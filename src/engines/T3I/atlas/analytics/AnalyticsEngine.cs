namespace Whycespace.Engines.T3I.Atlas.Analytics;

using Whycespace.Contracts.Engines;
using Whycespace.Runtime.EngineManifest.Attributes;
using Whycespace.Runtime.EngineManifest.Models;

[EngineManifest("Analytics", EngineTier.T3I, EngineKind.Projection, "AnalyticsRequest", typeof(EngineEvent))]
public sealed class AnalyticsEngine : IEngine
{
    public string Name => "Analytics";

    public Task<EngineResult> ExecuteAsync(EngineContext context)
    {
        var analysisType = context.Data.GetValueOrDefault("analysisType") as string;
        if (string.IsNullOrEmpty(analysisType))
            return Task.FromResult(EngineResult.Fail("Missing analysisType"));

        return analysisType switch
        {
            "ClusterPerformance" => AnalyzeClusterPerformance(context),
            "SpvHealth" => AnalyzeSpvHealth(context),
            "RevenueBreakdown" => AnalyzeRevenueBreakdown(context),
            "WorkflowEfficiency" => AnalyzeWorkflowEfficiency(context),
            "DriverUtilization" => AnalyzeDriverUtilization(context),
            "PropertyOccupancy" => AnalyzePropertyOccupancy(context),
            _ => Task.FromResult(EngineResult.Fail($"Unknown analysisType: {analysisType}"))
        };
    }

    private static Task<EngineResult> AnalyzeClusterPerformance(EngineContext context)
    {
        var clusterId = context.Data.GetValueOrDefault("clusterId") as string;
        if (string.IsNullOrEmpty(clusterId))
            return Task.FromResult(EngineResult.Fail("Missing clusterId"));

        var totalRevenue = ResolveDecimal(context.Data.GetValueOrDefault("totalRevenue")) ?? 0m;
        var totalCosts = ResolveDecimal(context.Data.GetValueOrDefault("totalCosts")) ?? 0m;
        var activeSpvCount = ResolveInt(context.Data.GetValueOrDefault("activeSpvCount")) ?? 0;
        var transactionCount = ResolveInt(context.Data.GetValueOrDefault("transactionCount")) ?? 0;

        var profitMargin = totalRevenue > 0
            ? Math.Round((totalRevenue - totalCosts) / totalRevenue * 100, 2)
            : 0m;

        var revenuePerSpv = activeSpvCount > 0
            ? Math.Round(totalRevenue / activeSpvCount, 2)
            : 0m;

        var performanceRating = profitMargin switch
        {
            > 30 => "excellent",
            > 15 => "good",
            > 5 => "moderate",
            > 0 => "low",
            _ => "loss"
        };

        var events = new[]
        {
            EngineEvent.Create("ClusterPerformanceAnalyzed", Guid.Parse(context.WorkflowId),
                new Dictionary<string, object>
                {
                    ["clusterId"] = clusterId,
                    ["totalRevenue"] = totalRevenue,
                    ["totalCosts"] = totalCosts,
                    ["profitMargin"] = profitMargin,
                    ["revenuePerSpv"] = revenuePerSpv,
                    ["activeSpvCount"] = activeSpvCount,
                    ["transactionCount"] = transactionCount,
                    ["performanceRating"] = performanceRating,
                    ["topic"] = "whyce.cluster.events"
                })
        };

        return Task.FromResult(EngineResult.Ok(events,
            new Dictionary<string, object>
            {
                ["clusterId"] = clusterId,
                ["profitMargin"] = profitMargin,
                ["revenuePerSpv"] = revenuePerSpv,
                ["performanceRating"] = performanceRating
            }));
    }

    private static Task<EngineResult> AnalyzeSpvHealth(EngineContext context)
    {
        var spvId = context.Data.GetValueOrDefault("spvId") as string;
        if (string.IsNullOrEmpty(spvId))
            return Task.FromResult(EngineResult.Fail("Missing spvId"));

        var allocatedCapital = ResolveDecimal(context.Data.GetValueOrDefault("allocatedCapital")) ?? 0m;
        var totalAssetValue = ResolveDecimal(context.Data.GetValueOrDefault("totalAssetValue")) ?? 0m;
        var totalRevenue = ResolveDecimal(context.Data.GetValueOrDefault("totalRevenue")) ?? 0m;
        var totalDistributed = ResolveDecimal(context.Data.GetValueOrDefault("totalDistributed")) ?? 0m;

        var capitalEfficiency = allocatedCapital > 0
            ? Math.Round(totalRevenue / allocatedCapital * 100, 2)
            : 0m;

        var assetCoverage = allocatedCapital > 0
            ? Math.Round(totalAssetValue / allocatedCapital * 100, 2)
            : 0m;

        var healthStatus = capitalEfficiency switch
        {
            > 100 => "strong",
            > 50 => "healthy",
            > 20 => "developing",
            > 0 => "underperforming",
            _ => "inactive"
        };

        var events = new[]
        {
            EngineEvent.Create("SpvHealthAnalyzed", Guid.Parse(context.WorkflowId),
                new Dictionary<string, object>
                {
                    ["spvId"] = spvId,
                    ["allocatedCapital"] = allocatedCapital,
                    ["totalAssetValue"] = totalAssetValue,
                    ["totalRevenue"] = totalRevenue,
                    ["totalDistributed"] = totalDistributed,
                    ["capitalEfficiency"] = capitalEfficiency,
                    ["assetCoverage"] = assetCoverage,
                    ["healthStatus"] = healthStatus,
                    ["topic"] = "whyce.spv.events"
                })
        };

        return Task.FromResult(EngineResult.Ok(events,
            new Dictionary<string, object>
            {
                ["spvId"] = spvId,
                ["capitalEfficiency"] = capitalEfficiency,
                ["assetCoverage"] = assetCoverage,
                ["healthStatus"] = healthStatus
            }));
    }

    private static Task<EngineResult> AnalyzeRevenueBreakdown(EngineContext context)
    {
        var spvId = context.Data.GetValueOrDefault("spvId") as string;
        if (string.IsNullOrEmpty(spvId))
            return Task.FromResult(EngineResult.Fail("Missing spvId"));

        var fareRevenue = ResolveDecimal(context.Data.GetValueOrDefault("fareRevenue")) ?? 0m;
        var rentalRevenue = ResolveDecimal(context.Data.GetValueOrDefault("rentalRevenue")) ?? 0m;
        var serviceRevenue = ResolveDecimal(context.Data.GetValueOrDefault("serviceRevenue")) ?? 0m;
        var otherRevenue = ResolveDecimal(context.Data.GetValueOrDefault("otherRevenue")) ?? 0m;

        var totalRevenue = fareRevenue + rentalRevenue + serviceRevenue + otherRevenue;

        decimal Pct(decimal part) => totalRevenue > 0 ? Math.Round(part / totalRevenue * 100, 2) : 0m;

        var dominantSource = (fareRevenue, rentalRevenue, serviceRevenue, otherRevenue) switch
        {
            var (f, r, s, o) when f >= r && f >= s && f >= o => "fare",
            var (_, r, s, o) when r >= s && r >= o => "rental",
            var (_, _, s, o) when s >= o => "service",
            _ => "other"
        };

        var events = new[]
        {
            EngineEvent.Create("RevenueBreakdownAnalyzed", Guid.Parse(context.WorkflowId),
                new Dictionary<string, object>
                {
                    ["spvId"] = spvId,
                    ["totalRevenue"] = totalRevenue,
                    ["fareRevenue"] = fareRevenue,
                    ["farePercent"] = Pct(fareRevenue),
                    ["rentalRevenue"] = rentalRevenue,
                    ["rentalPercent"] = Pct(rentalRevenue),
                    ["serviceRevenue"] = serviceRevenue,
                    ["servicePercent"] = Pct(serviceRevenue),
                    ["otherRevenue"] = otherRevenue,
                    ["otherPercent"] = Pct(otherRevenue),
                    ["dominantSource"] = dominantSource,
                    ["topic"] = "whyce.economic.events"
                })
        };

        return Task.FromResult(EngineResult.Ok(events,
            new Dictionary<string, object>
            {
                ["spvId"] = spvId,
                ["totalRevenue"] = totalRevenue,
                ["dominantSource"] = dominantSource
            }));
    }

    private static Task<EngineResult> AnalyzeWorkflowEfficiency(EngineContext context)
    {
        var workflowName = context.Data.GetValueOrDefault("workflowName") as string;
        if (string.IsNullOrEmpty(workflowName))
            return Task.FromResult(EngineResult.Fail("Missing workflowName"));

        var totalExecutions = ResolveInt(context.Data.GetValueOrDefault("totalExecutions")) ?? 0;
        var successfulExecutions = ResolveInt(context.Data.GetValueOrDefault("successfulExecutions")) ?? 0;
        var avgDurationMs = ResolveInt(context.Data.GetValueOrDefault("avgDurationMs")) ?? 0;

        var successRate = totalExecutions > 0
            ? Math.Round((decimal)successfulExecutions / totalExecutions * 100, 2)
            : 0m;

        var efficiencyRating = (successRate, avgDurationMs) switch
        {
            ( > 95, < 500) => "optimal",
            ( > 90, < 1000) => "efficient",
            ( > 80, _) => "acceptable",
            _ => "needs-improvement"
        };

        var events = new[]
        {
            EngineEvent.Create("WorkflowEfficiencyAnalyzed", Guid.Parse(context.WorkflowId),
                new Dictionary<string, object>
                {
                    ["workflowName"] = workflowName,
                    ["totalExecutions"] = totalExecutions,
                    ["successfulExecutions"] = successfulExecutions,
                    ["successRate"] = successRate,
                    ["avgDurationMs"] = avgDurationMs,
                    ["efficiencyRating"] = efficiencyRating,
                    ["topic"] = "whyce.system.events"
                })
        };

        return Task.FromResult(EngineResult.Ok(events,
            new Dictionary<string, object>
            {
                ["workflowName"] = workflowName,
                ["successRate"] = successRate,
                ["efficiencyRating"] = efficiencyRating
            }));
    }

    private static Task<EngineResult> AnalyzeDriverUtilization(EngineContext context)
    {
        var clusterId = context.Data.GetValueOrDefault("clusterId") as string ?? "whyce-mobility";
        var totalDrivers = ResolveInt(context.Data.GetValueOrDefault("totalDrivers")) ?? 0;
        var activeDrivers = ResolveInt(context.Data.GetValueOrDefault("activeDrivers")) ?? 0;
        var totalTrips = ResolveInt(context.Data.GetValueOrDefault("totalTrips")) ?? 0;

        var utilizationRate = totalDrivers > 0
            ? Math.Round((decimal)activeDrivers / totalDrivers * 100, 2)
            : 0m;

        var tripsPerDriver = activeDrivers > 0
            ? Math.Round((decimal)totalTrips / activeDrivers, 2)
            : 0m;

        var events = new[]
        {
            EngineEvent.Create("DriverUtilizationAnalyzed", Guid.Parse(context.WorkflowId),
                new Dictionary<string, object>
                {
                    ["clusterId"] = clusterId,
                    ["totalDrivers"] = totalDrivers,
                    ["activeDrivers"] = activeDrivers,
                    ["totalTrips"] = totalTrips,
                    ["utilizationRate"] = utilizationRate,
                    ["tripsPerDriver"] = tripsPerDriver,
                    ["topic"] = "whyce.cluster.events"
                })
        };

        return Task.FromResult(EngineResult.Ok(events,
            new Dictionary<string, object>
            {
                ["clusterId"] = clusterId,
                ["utilizationRate"] = utilizationRate,
                ["tripsPerDriver"] = tripsPerDriver
            }));
    }

    private static Task<EngineResult> AnalyzePropertyOccupancy(EngineContext context)
    {
        var clusterId = context.Data.GetValueOrDefault("clusterId") as string ?? "whyce-property";
        var totalListings = ResolveInt(context.Data.GetValueOrDefault("totalListings")) ?? 0;
        var occupiedListings = ResolveInt(context.Data.GetValueOrDefault("occupiedListings")) ?? 0;
        var avgMonthlyRent = ResolveDecimal(context.Data.GetValueOrDefault("avgMonthlyRent")) ?? 0m;

        var occupancyRate = totalListings > 0
            ? Math.Round((decimal)occupiedListings / totalListings * 100, 2)
            : 0m;

        var vacancyCount = totalListings - occupiedListings;

        var events = new[]
        {
            EngineEvent.Create("PropertyOccupancyAnalyzed", Guid.Parse(context.WorkflowId),
                new Dictionary<string, object>
                {
                    ["clusterId"] = clusterId,
                    ["totalListings"] = totalListings,
                    ["occupiedListings"] = occupiedListings,
                    ["vacancyCount"] = vacancyCount,
                    ["occupancyRate"] = occupancyRate,
                    ["avgMonthlyRent"] = avgMonthlyRent,
                    ["topic"] = "whyce.cluster.events"
                })
        };

        return Task.FromResult(EngineResult.Ok(events,
            new Dictionary<string, object>
            {
                ["clusterId"] = clusterId,
                ["occupancyRate"] = occupancyRate,
                ["vacancyCount"] = vacancyCount
            }));
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
}
