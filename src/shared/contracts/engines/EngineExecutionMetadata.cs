namespace Whycespace.Contracts.Engines;

public sealed record EngineExecutionMetadata(
    string EngineName,
    string Version,
    DateTimeOffset StartedAt,
    DateTimeOffset CompletedAt,
    TimeSpan Duration,
    int EventsProduced
);
