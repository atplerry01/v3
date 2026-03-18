namespace Whycespace.Systems.Midstream.WSS.Policies;

public sealed record RetryDecision(
    bool ShouldRetry,
    TimeSpan RetryDelay,
    FailureAction FailureAction
);
