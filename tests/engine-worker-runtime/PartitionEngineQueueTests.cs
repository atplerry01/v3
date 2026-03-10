using Whycespace.Contracts.Engines;
using Whycespace.Contracts.Primitives;
using Whycespace.EngineWorkerRuntime.Queue;

namespace Whycespace.EngineWorkerRuntime.Tests;

public class PartitionEngineQueueTests
{
    [Fact]
    public async Task Enqueue_And_Dequeue_Returns_Same_Invocation()
    {
        var registry = new PartitionEngineQueueRegistry(4);

        var envelope = new EngineInvocationEnvelope(
            Guid.NewGuid(),
            "TestEngine",
            "wf-1",
            "step-1",
            new PartitionKey("key-1"),
            new Dictionary<string, object>()
        );

        var writer = registry.GetWriter(0);
        await writer.WriteAsync(envelope);

        var reader = registry.GetReader(0);
        var success = reader.TryRead(out var result);

        Assert.True(success);
        Assert.Equal(envelope.InvocationId, result!.InvocationId);
        Assert.Equal("TestEngine", result.EngineName);
    }

    [Fact]
    public void PartitionCount_Matches_Constructor()
    {
        var registry = new PartitionEngineQueueRegistry(8);
        Assert.Equal(8, registry.PartitionCount);
    }
}
