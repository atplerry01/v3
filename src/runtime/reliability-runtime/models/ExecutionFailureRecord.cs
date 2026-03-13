namespace Whycespace.ReliabilityRuntime.Models;

public sealed class ExecutionFailureRecord
{
    public string ExecutionId { get; }

    public string Reason { get; }

    public DateTime TimestampUtc { get; }

    public ExecutionFailureRecord(string executionId, string reason)
    {
        ExecutionId = executionId;
        Reason = reason;
        TimestampUtc = DateTime.UtcNow;
    }
}
