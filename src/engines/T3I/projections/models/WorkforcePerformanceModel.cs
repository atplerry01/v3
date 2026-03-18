namespace Whycespace.Engines.T3I.Projections.Models;

public sealed record WorkforcePerformanceModel(
    Guid WorkerId,
    int TasksCompleted,
    int TasksAssigned,
    double CompletionRate,
    string CurrentStatus,
    DateTimeOffset LastUpdatedAt);
