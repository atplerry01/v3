namespace Whycespace.SimulationRuntime.Models;

public sealed record SimulationTrace(
    string StepName,
    string Description,
    DateTimeOffset Timestamp
);
