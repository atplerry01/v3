using Whycespace.ReliabilityRuntime.Dlq;
using Whycespace.ReliabilityRuntime.Models;

namespace Whycespace.ReliabilityRuntime.Tests;

public sealed class DeadLetterQueueManagerTests
{
    [Fact]
    public void Add_StoresRecord()
    {
        var manager = new DeadLetterQueueManager();
        var record = new ExecutionFailureRecord("exec-1", "timeout");

        manager.Add(record);

        Assert.Single(manager.GetAll());
    }

    [Fact]
    public void GetAll_ReturnsAllRecords()
    {
        var manager = new DeadLetterQueueManager();
        manager.Add(new ExecutionFailureRecord("exec-1", "timeout"));
        manager.Add(new ExecutionFailureRecord("exec-2", "error"));

        var records = manager.GetAll();

        Assert.Equal(2, records.Count);
    }
}
