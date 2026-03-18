namespace Whycespace.SimulationRuntime.Models;

public sealed record SimulationStateSnapshot(
    IReadOnlyList<string> AffectedAggregates,
    int CapturedEventCount,
    int PolicyEvaluations,
    DateTimeOffset SimulatedAt
);
