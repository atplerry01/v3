
using Whycespace.Shared.Primitives.Common;
using Whycespace.Shared.Envelopes;
using Whycespace.Contracts.Events;
using Whycespace.EventIdempotency.Guard;
using Whycespace.EventIdempotency.Registry;

namespace Whycespace.EventIdempotency.Tests;

public class EventProcessingGuardTests
{
    [Fact]
    public void ShouldProcess_Returns_True_For_New_Event()
    {
        var registry = new EventDeduplicationRegistry();
        var guard = new EventProcessingGuard(registry);

        var envelope = CreateEnvelope(Guid.NewGuid());

        Assert.True(guard.ShouldProcess(envelope));
    }

    [Fact]
    public void ShouldProcess_Returns_False_For_Duplicate_Event()
    {
        var registry = new EventDeduplicationRegistry();
        var guard = new EventProcessingGuard(registry);

        var eventId = Guid.NewGuid();
        var envelope = CreateEnvelope(eventId);

        guard.ShouldProcess(envelope);
        Assert.False(guard.ShouldProcess(envelope));
    }

    [Fact]
    public void ShouldProcess_Replay_Safety_Same_EventId_Rejected()
    {
        var registry = new EventDeduplicationRegistry();
        var guard = new EventProcessingGuard(registry);

        var eventId = Guid.NewGuid();

        Assert.True(guard.ShouldProcess(CreateEnvelope(eventId)));
        Assert.False(guard.ShouldProcess(CreateEnvelope(eventId)));
        Assert.False(guard.ShouldProcess(CreateEnvelope(eventId)));
    }

    private static EventEnvelope CreateEnvelope(Guid eventId) =>
        new(
            eventId,
            "DriverMatchedEvent",
            "whyce.engine.events",
            new { DriverId = "d-1" },
            new PartitionKey("pk-1"),
            Timestamp.Now()
        );
}
