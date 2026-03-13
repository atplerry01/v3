namespace Whycespace.Engines.T3I.HEOS;

public sealed record WorkforcePerformanceCommand(
    Guid WorkforceId,
    int CompletedTasks,
    int FailedTasks,
    decimal AverageTaskDuration,
    decimal CustomerRating,
    DateTimeOffset EvaluationPeriodStart,
    DateTimeOffset EvaluationPeriodEnd
);
