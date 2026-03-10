namespace Whycespace.SimulationRuntime.Runtime;

using Whycespace.SimulationRuntime.Models;

public sealed class SimulationRuntimeEngine
{
    private const decimal DefaultRevenueGrowthRate = 0.10m;
    private const decimal DefaultAssetAppreciationRate = 0.05m;
    private const decimal DefaultCostRatio = 0.60m;

    public SimulationResult RunSimulation(SimulationScenario scenario)
    {
        var totalCapital = scenario.SpvCount * scenario.CapitalPerSpv;

        var projectedAssets = SimulateAssetGrowth(totalCapital, scenario.DurationYears);
        var projectedRevenue = SimulateRevenueGrowth(totalCapital, scenario.DurationYears);
        var projectedProfit = projectedRevenue * (1m - DefaultCostRatio);

        return new SimulationResult(
            ScenarioId: scenario.ScenarioId,
            ProjectedAssets: Math.Round(projectedAssets, 2),
            ProjectedRevenue: Math.Round(projectedRevenue, 2),
            ProjectedProfit: Math.Round(projectedProfit, 2)
        );
    }

    private static decimal SimulateAssetGrowth(decimal initialCapital, int years)
    {
        var value = initialCapital;
        for (var y = 0; y < years; y++)
            value += value * DefaultAssetAppreciationRate;
        return value;
    }

    private static decimal SimulateRevenueGrowth(decimal initialCapital, int years)
    {
        var annualRevenue = initialCapital * 0.20m;
        var totalRevenue = 0m;
        for (var y = 0; y < years; y++)
        {
            totalRevenue += annualRevenue;
            annualRevenue += annualRevenue * DefaultRevenueGrowthRate;
        }
        return totalRevenue;
    }
}
