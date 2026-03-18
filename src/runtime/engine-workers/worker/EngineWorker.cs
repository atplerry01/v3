
using System.Threading.Channels;
using Whycespace.Shared.Envelopes;
using Whycespace.Contracts.Engines;
using Whycespace.Shared.Primitives.Common;
using Whycespace.EngineRuntime.Resolver;
using Whycespace.Contracts.Events;
using Whycespace.EventFabric.Publisher;
using Whycespace.EventFabric.Topics;

namespace Whycespace.EngineWorkerRuntime.Worker;

public sealed class EngineWorker
{
    private readonly EngineResolver _resolver;
    private readonly ChannelReader<EngineInvocationEnvelope> _reader;
    private readonly IEventPublisher? _eventPublisher;
    private readonly int _workerId;
    private readonly int _partitionId;

    public EngineWorker(
        int workerId,
        int partitionId,
        EngineResolver resolver,
        ChannelReader<EngineInvocationEnvelope> reader,
        IEventPublisher? eventPublisher = null)
    {
        _workerId = workerId;
        _partitionId = partitionId;
        _resolver = resolver;
        _reader = reader;
        _eventPublisher = eventPublisher;
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

                var result = await engine.ExecuteAsync(context);

                if (_eventPublisher is not null)
                {
                    foreach (var evt in result.Events)
                    {
                        await _eventPublisher.PublishAsync(
                            EventTopics.EngineEvents,
                            new EventEnvelope(
                                Guid.NewGuid(),
                                evt.EventType,
                                EventTopics.EngineEvents,
                                evt,
                                context.PartitionKey,
                                Timestamp.Now()
                            ),
                            cancellationToken
                        );
                    }
                }
            }
        }
        finally
        {
            IsRunning = false;
        }
    }
}
