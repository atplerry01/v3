namespace Whycespace.System.Midstream.WSS.Models;

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
    string? CompensationStepId
);
