namespace Whycespace.ReliabilityRuntime.Timeout;

public sealed class WorkflowTimeoutManager
{
    private readonly Dictionary<string, DateTime> _workflowStart = new();

    public void Start(string workflowId)
    {
        _workflowStart[workflowId] = DateTime.UtcNow;
    }

    public bool HasTimedOut(string workflowId, TimeSpan timeout)
    {
        if (!_workflowStart.TryGetValue(workflowId, out var start))
            return false;

        return DateTime.UtcNow - start > timeout;
    }

    public int TrackedCount => _workflowStart.Count;
}
