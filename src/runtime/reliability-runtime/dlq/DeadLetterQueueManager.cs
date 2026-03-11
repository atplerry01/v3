namespace Whycespace.ReliabilityRuntime.Dlq;

using Whycespace.ReliabilityRuntime.Models;

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
