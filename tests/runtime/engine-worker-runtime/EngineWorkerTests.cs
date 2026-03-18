using System.Threading.Channels;
using Whycespace.Shared.Envelopes;
using Whycespace.Contracts.Engines;
using Whycespace.Shared.Primitives.Common;
using Whycespace.EngineRuntime.Registry;
using Whycespace.EngineRuntime.Resolver;
using Whycespace.EngineWorkerRuntime.Worker;

namespace Whycespace.EngineWorkerRuntime.Tests;

public class EngineWorkerTests
{
    [Fact]
    public async Task Worker_Executes_Engine_From_Queue()
    {
        var stubEngine = new StubEngine("TestEngine");
        var registry = new EngineRegistry();
        registry.Register(stubEngine);
        var resolver = new EngineResolver(registry);

        var channel = Channel.CreateUnbounded<EngineInvocationEnvelope>();

        var worker = new EngineWorker(0, 0, resolver, channel.Reader);

        var envelope = new EngineInvocationEnvelope(
            Guid.NewGuid(),
            "TestEngine",
            "wf-1",
            "step-1",
            new PartitionKey("key-1"),
            new Dictionary<string, object>()
        );

        await channel.Writer.WriteAsync(envelope);
        channel.Writer.Complete();

        await worker.RunAsync(CancellationToken.None);

        Assert.Equal(1, stubEngine.ExecutionCount);
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
