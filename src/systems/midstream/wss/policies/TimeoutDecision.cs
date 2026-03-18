namespace Whycespace.Systems.Midstream.WSS.Policies;

public sealed record TimeoutDecision(
    bool IsTimeout,
    string InstanceId,
    string StepId,
    TimeSpan TimeoutDuration,
    TimeSpan ExceededBy
);
