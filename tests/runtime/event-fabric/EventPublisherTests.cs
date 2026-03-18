
using Confluent.Kafka;
using Whycespace.Shared.Envelopes;
using Whycespace.Shared.Primitives.Common;
using WTimestamp = Whycespace.Shared.Primitives.Common.Timestamp;
using Whycespace.Contracts.Events;
using Whycespace.EventFabric.Publisher;
using Whycespace.EventFabric.Topics;

namespace Whycespace.EventFabric.Tests;

public class EventPublisherTests
{
    [Fact]
    public async Task PublishAsync_Sends_Message_To_Topic()
    {
        var produced = new List<(string Topic, Message<string, string> Message)>();
        var mockProducer = new StubProducer(produced);
        var publisher = new KafkaEventPublisher(mockProducer);

        var envelope = new EventEnvelope(
            Guid.NewGuid(),
            "DriverMatchedEvent",
            EventTopics.EngineEvents,
            new { DriverId = "d-1" },
            new PartitionKey("pk-1"),
            WTimestamp.Now()
        );

        await publisher.PublishAsync(EventTopics.EngineEvents, envelope, CancellationToken.None);

        Assert.Single(produced);
        Assert.Equal(EventTopics.EngineEvents, produced[0].Topic);
        Assert.Equal("pk-1", produced[0].Message.Key);
        Assert.Contains("DriverMatchedEvent", produced[0].Message.Value);
    }

    private sealed class StubProducer : IProducer<string, string>
    {
        private readonly List<(string Topic, Message<string, string> Message)> _produced;

        public StubProducer(List<(string, Message<string, string>)> produced)
            => _produced = produced;

        public Handle Handle => throw new NotImplementedException();
        public string Name => "stub";

        public Task<DeliveryResult<string, string>> ProduceAsync(
            string topic,
            Message<string, string> message,
            CancellationToken cancellationToken = default)
        {
            _produced.Add((topic, message));
            return Task.FromResult(new DeliveryResult<string, string>
            {
                Topic = topic,
                Message = message,
                Status = PersistenceStatus.Persisted
            });
        }

        public Task<DeliveryResult<string, string>> ProduceAsync(
            TopicPartition topicPartition,
            Message<string, string> message,
            CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public void Produce(string topic, Message<string, string> message, Action<DeliveryReport<string, string>>? deliveryHandler = null)
            => throw new NotImplementedException();

        public void Produce(TopicPartition topicPartition, Message<string, string> message, Action<DeliveryReport<string, string>>? deliveryHandler = null)
            => throw new NotImplementedException();

        public int Poll(TimeSpan timeout) => 0;
        public int Flush(TimeSpan timeout) => 0;
        public void Flush(CancellationToken cancellationToken = default) { }
        public void InitTransactions(TimeSpan timeout) => throw new NotImplementedException();
        public void BeginTransaction() => throw new NotImplementedException();
        public void CommitTransaction(TimeSpan timeout) => throw new NotImplementedException();
        public void CommitTransaction() => throw new NotImplementedException();
        public void AbortTransaction(TimeSpan timeout) => throw new NotImplementedException();
        public void AbortTransaction() => throw new NotImplementedException();
        public void SendOffsetsToTransaction(IEnumerable<TopicPartitionOffset> offsets, IConsumerGroupMetadata groupMetadata, TimeSpan timeout) => throw new NotImplementedException();
        public int AddBrokers(string brokers) => 0;
        public void SetSaslCredentials(string username, string password) => throw new NotImplementedException();
        public void Dispose() { }
    }
}
