namespace Whycespace.SimulationRuntime.Models;

public sealed record SimulationScenario(
    Guid ScenarioId,
    string ClusterName,
    int SpvCount,
    decimal CapitalPerSpv,
    int DurationYears
);
