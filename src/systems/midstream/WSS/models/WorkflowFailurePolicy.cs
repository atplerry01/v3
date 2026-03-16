namespace Whycespace.Systems.Midstream.WSS.Models;

public enum FailureAction
{
    Fail,
    Retry,
    Skip,
    Compensate
}

public sealed record WorkflowFailurePolicy(
    FailureAction Action,
    int MaxRetries,
    TimeSpan RetryDelay,
    string? CompensationStepId
);
