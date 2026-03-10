namespace Whycespace.Reliability.Saga;

public sealed record SagaStep(
    string Name,
    Func<Task<bool>> Execute,
    Func<Task> Compensate
);
