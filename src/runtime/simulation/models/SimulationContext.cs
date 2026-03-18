namespace Whycespace.SimulationRuntime.Models;

public sealed record SimulationContext(
    Guid SimulationId,
    SimulationCommand Command,
    DateTimeOffset StartedAt
);
