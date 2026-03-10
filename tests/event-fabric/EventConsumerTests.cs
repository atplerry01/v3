using System.Text.Json;
using Confluent.Kafka;
using Whycespace.Contracts.Primitives;
using WTimestamp = Whycespace.Contracts.Primitives.Timestamp;
using Whycespace.EventFabric.Consumer;
using Whycespace.EventFabric.Models;
using Whycespace.EventFabric.Router;
using Whycespace.EventFabric.Topics;

namespace Whycespace.EventFabric.Tests;

public class EventConsumerTests
{
    [Fact]
    public async Task StartAsync_Consumes_And_Routes_Event()
    {
        var envelope = new EventEnvelope(
            Guid.NewGuid(),
            "TestEvent",
            EventTopics.EngineEvents,
            new { Data = "test" },
            new PartitionKey("pk-1"),
            WTimestamp.Now()
        );

        var json = JsonSerializer.Serialize(envelope);

        var routed = new List<EventEnvelope>();
        var router = new EventRouter();
        router.Register(EventTopics.EngineEvents, e =>
        {
            routed.Add(e);
            return Task.CompletedTask;
        });

        var stubConsumer = new StubConsumer(json);
        var consumer = new KafkaEventConsumer(
            stubConsumer,
            [EventTopics.EngineEvents],
            router
        );

        using var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromMilliseconds(100));

        try
        {
            await consumer.StartAsync(cts.Token);
        }
        catch (OperationCanceledException)
        {
            // Expected
        }

        Assert.Single(routed);
        Assert.Equal("TestEvent", routed[0].EventType);
    }

    private sealed class StubConsumer : IConsumer<string, string>
    {
        private readonly string _json;
        private bool _consumed;

        public StubConsumer(string json) => _json = json;

        public string MemberId => "stub";
        public List<TopicPartition> Assignment => [];
        public List<string> Subscription => [];
        public IConsumerGroupMetadata ConsumerGroupMetadata => throw new NotImplementedException();
        public Handle Handle => throw new NotImplementedException();
        public string Name => "stub";

        public ConsumeResult<string, string> Consume(int millisecondsTimeout) => Consume(CancellationToken.None);
        public ConsumeResult<string, string> Consume(TimeSpan timeout) => Consume(CancellationToken.None);
        public ConsumeResult<string, string> Consume(CancellationToken cancellationToken = default)
        {
            if (_consumed)
            {
                cancellationToken.WaitHandle.WaitOne();
                cancellationToken.ThrowIfCancellationRequested();
                return null!;
            }

            _consumed = true;
            return new ConsumeResult<string, string>
            {
                Message = new Message<string, string> { Key = "pk-1", Value = _json },
                Topic = EventTopics.EngineEvents
            };
        }

        public void Subscribe(IEnumerable<string> topics) { }
        public void Subscribe(string topic) { }
        public void Unsubscribe() { }
        public void Assign(TopicPartition partition) { }
        public void Assign(TopicPartitionOffset partition) { }
        public void Assign(IEnumerable<TopicPartition> partitions) { }
        public void Assign(IEnumerable<TopicPartitionOffset> partitions) { }
        public void IncrementalAssign(IEnumerable<TopicPartitionOffset> partitions) { }
        public void IncrementalAssign(IEnumerable<TopicPartition> partitions) { }
        public void IncrementalUnassign(IEnumerable<TopicPartition> partitions) { }
        public void Unassign() { }
        public void StoreOffset(ConsumeResult<string, string> result) { }
        public void StoreOffset(TopicPartitionOffset offset) { }
        public List<TopicPartitionOffset> Commit() => [];
        public void Commit(IEnumerable<TopicPartitionOffset> offsets) { }
        public void Commit(ConsumeResult<string, string> result) { }
        public void Seek(TopicPartitionOffset tpo) { }
        public void Pause(IEnumerable<TopicPartition> partitions) { }
        public void Resume(IEnumerable<TopicPartition> partitions) { }
        public List<TopicPartitionOffset> Committed(IEnumerable<TopicPartition> partitions, TimeSpan timeout) => [];
        public List<TopicPartitionOffset> Committed(TimeSpan timeout) => [];
        public Offset Position(TopicPartition partition) => Offset.Unset;
        public List<TopicPartitionOffset> OffsetsForTimes(IEnumerable<TopicPartitionTimestamp> timestampsToSearch, TimeSpan timeout) => [];
        public WatermarkOffsets GetWatermarkOffsets(TopicPartition topicPartition) => new(Offset.Unset, Offset.Unset);
        public WatermarkOffsets QueryWatermarkOffsets(TopicPartition topicPartition, TimeSpan timeout) => new(Offset.Unset, Offset.Unset);
        public int AddBrokers(string brokers) => 0;
        public void SetSaslCredentials(string username, string password) { }
        public void Close() { }
        public void Dispose() { }
    }
}
