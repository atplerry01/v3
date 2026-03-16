namespace Whycespace.Systems.Midstream.WSS.Models;

public sealed record RetryDecision(
    bool ShouldRetry,
    TimeSpan RetryDelay,
    FailureAction FailureAction
);
