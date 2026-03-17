namespace Whycespace.Engines.T3I.Atlas.Workforce;

public sealed record WorkforcePerformanceCommand(
    Guid WorkforceId,
    int CompletedTasks,
    int FailedTasks,
    decimal AverageTaskDuration,
    decimal CustomerRating,
    DateTimeOffset EvaluationPeriodStart,
    DateTimeOffset EvaluationPeriodEnd
);
