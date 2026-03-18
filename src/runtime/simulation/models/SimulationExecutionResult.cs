namespace Whycespace.SimulationRuntime.Models;

public sealed record SimulationExecutionResult(
    Guid SimulationId,
    SimulationCommand Command,
    IReadOnlyList<SimulationEventRecord> CapturedEvents,
    IReadOnlyList<SimulationTrace> Traces,
    SimulationStateSnapshot StateSnapshot,
    bool Success,
    DateTimeOffset CompletedAt,
    string? ErrorMessage = null
);
