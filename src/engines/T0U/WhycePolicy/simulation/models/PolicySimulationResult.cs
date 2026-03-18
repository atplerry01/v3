namespace Whycespace.Engines.T0U.WhycePolicy.Simulation.Models;

public sealed record PolicySimulationBatchResult(
    List<PolicySimulationRecord> SimulationRecords,
    int SimulationCount,
    DateTime GeneratedAt
);
