namespace Whycespace.Runtime.Persistence.Projections;

public sealed record ProjectionRecord(
    string ProjectionName,
    string Key,
    string State,
    DateTimeOffset UpdatedAt,
    long Version = 0
);
