namespace Whycespace.Systems.Downstream.Coordination.Trace;

public sealed class DownstreamTraceStep
{
    public string StepName { get; }
    public DateTimeOffset StartedAt { get; }
    public DateTimeOffset? CompletedAt { get; private set; }
    public bool IsSuccessful { get; private set; }
    public string? ErrorMessage { get; private set; }

    public DownstreamTraceStep(string stepName)
    {
        StepName = stepName;
        StartedAt = DateTimeOffset.UtcNow;
    }

    public void Complete()
    {
        CompletedAt = DateTimeOffset.UtcNow;
        IsSuccessful = true;
    }

    public void Fail(string error)
    {
        CompletedAt = DateTimeOffset.UtcNow;
        IsSuccessful = false;
        ErrorMessage = error;
    }
}
