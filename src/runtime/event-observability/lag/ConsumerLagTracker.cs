using System.Collections.Concurrent;

namespace Whycespace.EventObservability.Lag;

public sealed class ConsumerLagTracker
{
    private readonly ConcurrentDictionary<string, long> _kafkaOffsets = new();
    private readonly ConcurrentDictionary<string, long> _consumerOffsets = new();

    public void UpdateKafkaOffset(string topicPartition, long offset)
    {
        _kafkaOffsets[topicPartition] = offset;
    }

    public void UpdateConsumerOffset(string topicPartition, long offset)
    {
        _consumerOffsets[topicPartition] = offset;
    }

    public long GetLag(string topicPartition)
    {
        var kafkaOffset = _kafkaOffsets.GetValueOrDefault(topicPartition, 0);
        var consumerOffset = _consumerOffsets.GetValueOrDefault(topicPartition, 0);

        return Math.Max(0, kafkaOffset - consumerOffset);
    }

    public IReadOnlyDictionary<string, long> GetAllLag()
    {
        var lag = new Dictionary<string, long>();

        foreach (var kvp in _kafkaOffsets)
        {
            var consumerOffset = _consumerOffsets.GetValueOrDefault(kvp.Key, 0);
            lag[kvp.Key] = Math.Max(0, kvp.Value - consumerOffset);
        }

        return lag;
    }

    public long GetTotalLag()
    {
        long total = 0;

        foreach (var kvp in _kafkaOffsets)
        {
            var consumerOffset = _consumerOffsets.GetValueOrDefault(kvp.Key, 0);
            total += Math.Max(0, kvp.Value - consumerOffset);
        }

        return total;
    }
}
