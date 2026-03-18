namespace Whycespace.SimulationRuntime.Models;

public sealed record SimulationCommand(
    string CommandType,
    IReadOnlyDictionary<string, object> Payload,
    string? AggregateId = null,
    string? CorrelationId = null
);
