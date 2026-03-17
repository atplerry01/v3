namespace Whycespace.Engines.T0U.WhycePolicy.Simulation;

public sealed record PolicySimulationBatchResult(
    List<PolicySimulationRecord> SimulationRecords,
    int SimulationCount,
    DateTime GeneratedAt
);
