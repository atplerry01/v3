namespace Whycespace.Systems.Downstream.Coordination.Trace;

public sealed class DownstreamTraceContext
{
    public string TraceId { get; }
    public string OperationName { get; }
    public DateTimeOffset StartedAt { get; }
    public DateTimeOffset? CompletedAt { get; private set; }
    public bool IsSuccessful { get; private set; }
    private readonly List<DownstreamTraceStep> _steps = new();

    public IReadOnlyList<DownstreamTraceStep> Steps => _steps;

    public DownstreamTraceContext(string operationName)
    {
        TraceId = Guid.NewGuid().ToString();
        OperationName = operationName;
        StartedAt = DateTimeOffset.UtcNow;
    }

    public DownstreamTraceStep BeginStep(string stepName)
    {
        var step = new DownstreamTraceStep(stepName);
        _steps.Add(step);
        return step;
    }

    public void Complete(bool success)
    {
        CompletedAt = DateTimeOffset.UtcNow;
        IsSuccessful = success;
    }
}
