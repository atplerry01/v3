namespace Whycespace.Runtime.Observability.Workflow;

public sealed record WorkflowMetric(
    string WorkflowName,
    string StepName,
    long DurationMs,
    bool Success,
    DateTimeOffset Timestamp
);

public sealed class WorkflowMetrics
{
    private readonly List<WorkflowMetric> _metrics = new();

    public void Record(WorkflowMetric metric) => _metrics.Add(metric);

    public IReadOnlyList<WorkflowMetric> GetMetrics(string? workflowName = null)
    {
        return workflowName is null
            ? _metrics
            : _metrics.Where(m => m.WorkflowName == workflowName).ToList();
    }
}
