namespace Whycespace.Systems.Midstream.WSS.Models;

public sealed record TimeoutEntry(
    string InstanceId,
    string StepId,
    DateTimeOffset StartTime,
    TimeSpan TimeoutDuration
);
