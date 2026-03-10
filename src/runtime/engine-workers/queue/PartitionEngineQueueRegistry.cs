using System.Threading.Channels;
using Whycespace.Contracts.Engines;

namespace Whycespace.EngineWorkerRuntime.Queue;

public sealed class PartitionEngineQueueRegistry
{
    private readonly Dictionary<int, Channel<EngineInvocationEnvelope>> _queues;

    public PartitionEngineQueueRegistry(int partitionCount)
    {
        _queues = new Dictionary<int, Channel<EngineInvocationEnvelope>>();

        for (int i = 0; i < partitionCount; i++)
        {
            _queues[i] = Channel.CreateUnbounded<EngineInvocationEnvelope>();
        }
    }

    public ChannelWriter<EngineInvocationEnvelope> GetWriter(int partitionId)
    {
        return _queues[partitionId].Writer;
    }

    public ChannelReader<EngineInvocationEnvelope> GetReader(int partitionId)
    {
        return _queues[partitionId].Reader;
    }

    public int PartitionCount => _queues.Count;

    public int GetPendingCount(int partitionId)
    {
        return _queues[partitionId].Reader.CanCount
            ? _queues[partitionId].Reader.Count
            : 0;
    }
}
