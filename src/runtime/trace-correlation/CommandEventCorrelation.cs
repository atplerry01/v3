namespace Whycespace.Runtime.TraceCorrelation;

public sealed record CommandEventCorrelation(
    string CommandId,
    string EngineId,
    IReadOnlyList<string> EmittedEventIds,
    DateTimeOffset CorrelatedAt
);
