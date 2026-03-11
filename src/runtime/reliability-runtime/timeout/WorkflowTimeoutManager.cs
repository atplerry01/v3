namespace Whycespace.ReliabilityRuntime.Timeout;

/// <summary>
/// Thread Safety Notice
/// --------------------
/// This component is designed for single-threaded runtime access.
///
/// In the Whycespace runtime architecture, execution is serialized
/// through partition workers and workflow dispatchers.
///
/// Because of this guarantee, concurrent collections are not required
/// and standard Dictionary/List structures are used for efficiency.
///
/// If this component is used outside the partition runtime context,
/// external synchronization must be applied.
/// </summary>
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
