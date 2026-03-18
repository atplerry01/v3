namespace Whycespace.Shared.Projections;

public sealed record ProjectionMetadata(
    string ProjectionName,
    string Version,
    DateTimeOffset LastUpdated,
    long EventsProcessed,
    string? LastEventId = null
);
