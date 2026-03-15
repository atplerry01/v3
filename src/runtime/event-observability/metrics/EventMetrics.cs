using System.Collections.Concurrent;

namespace Whycespace.EventObservability.Metrics;

public sealed class EventMetrics
{
    private long _eventsPublishedTotal;
    private long _eventsConsumedTotal;
    private long _deadLetterEventsTotal;
    private readonly ConcurrentDictionary<string, long> _topicPublishCounts = new();
    private readonly ConcurrentDictionary<string, long> _topicConsumeCounts = new();
    private readonly ConcurrentDictionary<string, long> _partitionCounts = new();
    private readonly ConcurrentDictionary<string, double> _processingLatencies = new();

    public long EventsPublishedTotal => Interlocked.Read(ref _eventsPublishedTotal);
    public long EventsConsumedTotal => Interlocked.Read(ref _eventsConsumedTotal);
    public long DeadLetterEventsTotal => Interlocked.Read(ref _deadLetterEventsTotal);

    public void RecordPublished(string topic)
    {
        Interlocked.Increment(ref _eventsPublishedTotal);
        _topicPublishCounts.AddOrUpdate(topic, 1, (_, count) => count + 1);
    }

    public void RecordConsumed(string topic, string partitionKey)
    {
        Interlocked.Increment(ref _eventsConsumedTotal);
        _topicConsumeCounts.AddOrUpdate(topic, 1, (_, count) => count + 1);
        _partitionCounts.AddOrUpdate(partitionKey, 1, (_, count) => count + 1);
    }

    public void RecordDeadLetter()
    {
        Interlocked.Increment(ref _deadLetterEventsTotal);
    }

    public void RecordProcessingLatency(string topic, double latencyMs)
    {
        _processingLatencies[topic] = latencyMs;
    }

    public long GetTopicPublishCount(string topic)
    {
        return _topicPublishCounts.GetValueOrDefault(topic, 0);
    }

    public long GetTopicConsumeCount(string topic)
    {
        return _topicConsumeCounts.GetValueOrDefault(topic, 0);
    }

    public long GetPartitionThroughput(string partitionKey)
    {
        return _partitionCounts.GetValueOrDefault(partitionKey, 0);
    }

    public double GetProcessingLatency(string topic)
    {
        return _processingLatencies.GetValueOrDefault(topic, 0.0);
    }

    public IReadOnlyDictionary<string, long> GetTopicThroughput()
    {
        return _topicPublishCounts;
    }

    public IReadOnlyDictionary<string, long> GetPartitionThroughput()
    {
        return _partitionCounts;
    }
}
