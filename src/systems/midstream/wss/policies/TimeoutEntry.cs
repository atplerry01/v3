namespace Whycespace.Systems.Midstream.WSS.Policies;

public sealed record TimeoutEntry(
    string InstanceId,
    string StepId,
    DateTimeOffset StartTime,
    TimeSpan TimeoutDuration
);
