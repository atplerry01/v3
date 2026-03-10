using Whycespace.Contracts.Primitives;
using Whycespace.EventFabric.Models;
using Whycespace.EventFabric.Publisher;
using Whycespace.Reliability.Dlq;

namespace Whycespace.Reliability.Tests;

public class DeadLetterPublisherTests
{
    [Fact]
    public async Task PublishAsync_Sends_To_DLQ_Topic()
    {
        var published = new List<(string Topic, EventEnvelope Envelope)>();
        var stubPublisher = new StubEventPublisher(published);

        var dlqPublisher = new DeadLetterPublisher(stubPublisher);

        var originalEnvelope = new EventEnvelope(
            Guid.NewGuid(),
            "FailedEvent",
            "whyce.engine.events",
            new { Data = "test" },
            new PartitionKey("pk-1"),
            Timestamp.Now()
        );

        await dlqPublisher.PublishAsync(originalEnvelope, "Step failed", CancellationToken.None);

        Assert.Single(published);
        Assert.Equal(DeadLetterPublisher.DlqTopic, published[0].Topic);
        Assert.Contains("DeadLetter:", published[0].Envelope.EventType);
    }

    private sealed class StubEventPublisher : IEventPublisher
    {
        private readonly List<(string Topic, EventEnvelope Envelope)> _published;

        public StubEventPublisher(List<(string, EventEnvelope)> published) => _published = published;

        public Task PublishAsync(string topic, EventEnvelope envelope, CancellationToken cancellationToken)
        {
            _published.Add((topic, envelope));
            return Task.CompletedTask;
        }
    }
}
