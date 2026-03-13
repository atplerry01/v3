namespace Whycespace.ProjectionRebuild.Models;

public sealed record ProjectionCheckpoint(
    string ProjectionName,
    Guid LastProcessedEventId,
    DateTime Timestamp
);
