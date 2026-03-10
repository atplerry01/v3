namespace Whycespace.ReliabilityRuntime.Dlq;

using Whycespace.ReliabilityRuntime.Models;

public sealed class DeadLetterQueueManager
{
    private readonly List<ExecutionFailureRecord> _records = new();

    public void Add(ExecutionFailureRecord record)
    {
        _records.Add(record);
    }

    public IReadOnlyCollection<ExecutionFailureRecord> GetAll()
    {
        return _records.AsReadOnly();
    }
}
