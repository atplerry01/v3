using System.Threading.Channels;
using Whycespace.Contracts.Engines;
using Whycespace.EngineRuntime.Resolver;

namespace Whycespace.EngineWorkerRuntime.Worker;

public sealed class EngineWorker
{
    private readonly EngineResolver _resolver;
    private readonly ChannelReader<EngineInvocationEnvelope> _reader;
    private readonly int _workerId;
    private readonly int _partitionId;

    public EngineWorker(
        int workerId,
        int partitionId,
        EngineResolver resolver,
        ChannelReader<EngineInvocationEnvelope> reader)
    {
        _workerId = workerId;
        _partitionId = partitionId;
        _resolver = resolver;
        _reader = reader;
    }

    public int WorkerId => _workerId;
    public int PartitionId => _partitionId;
    public bool IsRunning { get; private set; }

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        IsRunning = true;

        try
        {
            await foreach (var invocation in _reader.ReadAllAsync(cancellationToken))
            {
                var engine = _resolver.Resolve(invocation.EngineName);

                var context = new EngineContext(
                    invocation.InvocationId,
                    invocation.WorkflowId,
                    invocation.WorkflowStep,
                    invocation.PartitionKey,
                    invocation.Context
                );

                await engine.ExecuteAsync(context);
            }
        }
        finally
        {
            IsRunning = false;
        }
    }
}
