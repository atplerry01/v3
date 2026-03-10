namespace Whycespace.SimulationRuntime.Models;

public sealed record SimulationResult(
    Guid ScenarioId,
    decimal ProjectedAssets,
    decimal ProjectedRevenue,
    decimal ProjectedProfit
);
