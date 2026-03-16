namespace Whycespace.Systems.Midstream.WSS.Models;

public sealed record TimeoutDecision(
    bool IsTimeout,
    string InstanceId,
    string StepId,
    TimeSpan TimeoutDuration,
    TimeSpan ExceededBy
);
