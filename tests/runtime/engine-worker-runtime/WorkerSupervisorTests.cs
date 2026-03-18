using System.Threading.Channels;
using Whycespace.Shared.Envelopes;
using Whycespace.Contracts.Engines;
using Whycespace.Shared.Primitives.Common;
using Whycespace.EngineRuntime.Registry;
using Whycespace.EngineRuntime.Resolver;
using Whycespace.EngineWorkerRuntime.Pool;
using Whycespace.EngineWorkerRuntime.Queue;
using Whycespace.EngineWorkerRuntime.Supervisor;

namespace Whycespace.EngineWorkerRuntime.Tests;

public class WorkerSupervisorTests
{
    [Fact]
    public async Task Supervisor_Starts_Workers()
    {
        var stubEngine = new StubEngine("TestEngine");
        var registry = new EngineRegistry();
        registry.Register(stubEngine);
        var resolver = new EngineResolver(registry);
        var queues = new PartitionEngineQueueRegistry(2);

        var pool = new PartitionEngineWorkerPool(2, 1, resolver, queues);
        var supervisor = new EngineWorkerSupervisor(pool);

        using var cts = new CancellationTokenSource();

        supervisor.Start(cts.Token);

        // Allow workers to start
        await Task.Delay(50);

        // Enqueue work to partition 0
        var envelope = new EngineInvocationEnvelope(
            Guid.NewGuid(),
            "TestEngine",
            "wf-1",
            "step-1",
            new PartitionKey("key-1"),
            new Dictionary<string, object>()
        );

        await queues.GetWriter(0).WriteAsync(envelope);

        // Allow processing
        await Task.Delay(100);

        Assert.Equal(1, stubEngine.ExecutionCount);

        cts.Cancel();
    }

    private sealed class StubEngine : IEngine
    {
        public string Name { get; }
        public int ExecutionCount { get; private set; }

        public StubEngine(string name) => Name = name;

        public Task<EngineResult> ExecuteAsync(EngineContext context)
        {
            ExecutionCount++;
            return Task.FromResult(EngineResult.Ok(Array.Empty<EngineEvent>()));
        }
    }
}
