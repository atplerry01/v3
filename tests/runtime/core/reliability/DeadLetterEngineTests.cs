
using Whycespace.Shared.Primitives.Common;
using Whycespace.Shared.Envelopes;
using Whycespace.Contracts.Events;
using Whycespace.Shared.Primitives.Common;
using Whycespace.Reliability.DeadLetter.Engine;
using Whycespace.Reliability.DeadLetter.Models;
using Whycespace.Reliability.Recovery.Engine;

namespace Whycespace.Reliability.Tests;

public sealed class DeadLetterEngineTests
{
    private readonly DeadLetterEngine _engine = new();
    private readonly EventRecoveryEngine _recoveryEngine = new();

    private static EventEnvelope CreateTestEnvelope(
        string eventType = "TestEvent",
        string topic = "whyce.test.events") =>
        new(
            EventId: Guid.NewGuid(),
            EventType: eventType,
            Topic: topic,
            Payload: new { Data = "test-payload" },
            PartitionKey: "test-key",
            Timestamp: Timestamp.Now()
        );

    [Fact]
    public void CreateDeadLetterEvent_RetryLimitExceeded_SetsCorrectReason()
    {
        var envelope = CreateTestEnvelope();

        var dlqEvent = _engine.CreateDeadLetterEvent(
            envelope, DeadLetterReason.RetryLimitExceeded, "Max retries reached", 3);

        Assert.Equal(DeadLetterReason.RetryLimitExceeded, dlqEvent.Reason);
        Assert.Equal(3, dlqEvent.RetryCount);
        Assert.Equal("Max retries reached", dlqEvent.ErrorMessage);
    }

    [Fact]
    public void CreateDeadLetterEvent_SchemaViolation_SetsCorrectReason()
    {
        var envelope = CreateTestEnvelope();

        var dlqEvent = _engine.CreateDeadLetterEvent(
            envelope, DeadLetterReason.SchemaViolation, "Missing required field", 0);

        Assert.Equal(DeadLetterReason.SchemaViolation, dlqEvent.Reason);
    }

    [Fact]
    public void CreateDeadLetterEvent_PreservesPayload()
    {
        var envelope = CreateTestEnvelope();

        var dlqEvent = _engine.CreateDeadLetterEvent(
            envelope, DeadLetterReason.EngineFailure, "Engine crashed", 1);

        Assert.NotNull(dlqEvent.Payload);
        Assert.NotEmpty(dlqEvent.Payload);
        Assert.Contains("test-payload", dlqEvent.Payload);
    }

    [Fact]
    public void CreateDeadLetterEvent_PreservesEventIdentity()
    {
        var envelope = CreateTestEnvelope("OrderPlaced", "whyce.economic.events");

        var dlqEvent = _engine.CreateDeadLetterEvent(
            envelope, DeadLetterReason.EngineFailure, "Error", 0);

        Assert.Equal(envelope.EventId, dlqEvent.EventId);
        Assert.Equal("OrderPlaced", dlqEvent.EventType);
        Assert.Equal("whyce.economic.events", dlqEvent.SourceTopic);
    }

    [Fact]
    public void CreateDeadLetterEvent_SetsFailedAtTimestamp()
    {
        var before = DateTime.UtcNow;
        var envelope = CreateTestEnvelope();

        var dlqEvent = _engine.CreateDeadLetterEvent(
            envelope, DeadLetterReason.InvalidPayload, "Bad data", 0);

        Assert.True(dlqEvent.FailedAt >= before);
        Assert.True(dlqEvent.FailedAt <= DateTime.UtcNow);
    }

    [Fact]
    public void CreateMetadata_SetsCorrectFields()
    {
        var envelope = CreateTestEnvelope();
        var dlqEvent = _engine.CreateDeadLetterEvent(
            envelope, DeadLetterReason.RetryLimitExceeded, "Error", 5);

        var metadata = _engine.CreateMetadata(dlqEvent);

        Assert.Equal(dlqEvent.EventId, metadata.EventId);
        Assert.Equal(5, metadata.RetryCount);
        Assert.Equal(dlqEvent.FailedAt, metadata.FirstFailure);
        Assert.Equal(dlqEvent.FailedAt, metadata.LastFailure);
    }

    [Fact]
    public void CreateMetadata_WithExistingFirstFailure_PreservesOriginalTimestamp()
    {
        var firstFailure = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var envelope = CreateTestEnvelope();
        var dlqEvent = _engine.CreateDeadLetterEvent(
            envelope, DeadLetterReason.RetryLimitExceeded, "Error", 2);

        var metadata = _engine.CreateMetadata(dlqEvent, firstFailure);

        Assert.Equal(firstFailure, metadata.FirstFailure);
        Assert.Equal(dlqEvent.FailedAt, metadata.LastFailure);
    }

    [Fact]
    public void RecoveryEngine_ExceedsReplayLimit_Quarantines()
    {
        var dlqEvent = CreateDlqEvent(DeadLetterReason.RetryLimitExceeded);

        var decision = _recoveryEngine.Evaluate(dlqEvent, replayCount: 3);

        Assert.False(decision.AllowReplay);
        Assert.True(decision.Quarantine);
    }

    [Fact]
    public void RecoveryEngine_SchemaViolation_Quarantines()
    {
        var dlqEvent = CreateDlqEvent(DeadLetterReason.SchemaViolation);

        var decision = _recoveryEngine.Evaluate(dlqEvent, replayCount: 0);

        Assert.False(decision.AllowReplay);
        Assert.True(decision.Quarantine);
        Assert.Contains("Schema violation", decision.Reason);
    }

    [Fact]
    public void RecoveryEngine_RetryLimitExceeded_AllowsReplay()
    {
        var dlqEvent = CreateDlqEvent(DeadLetterReason.RetryLimitExceeded);

        var decision = _recoveryEngine.Evaluate(dlqEvent, replayCount: 0);

        Assert.True(decision.AllowReplay);
        Assert.False(decision.Quarantine);
    }

    [Fact]
    public void RecoveryEngine_EngineFailure_AllowsReplay()
    {
        var dlqEvent = CreateDlqEvent(DeadLetterReason.EngineFailure);

        var decision = _recoveryEngine.Evaluate(dlqEvent, replayCount: 1);

        Assert.True(decision.AllowReplay);
        Assert.False(decision.Quarantine);
    }

    [Fact]
    public void RecoveryEngine_PolicyViolation_Quarantines()
    {
        var dlqEvent = CreateDlqEvent(DeadLetterReason.PolicyViolation);

        var decision = _recoveryEngine.Evaluate(dlqEvent, replayCount: 0);

        Assert.False(decision.AllowReplay);
        Assert.True(decision.Quarantine);
    }

    private static DeadLetterEvent CreateDlqEvent(DeadLetterReason reason) =>
        new(
            EventId: Guid.NewGuid(),
            EventType: "TestEvent",
            SourceTopic: "whyce.test.events",
            Partition: 0,
            Offset: 100,
            Reason: reason,
            ErrorMessage: "Test error",
            RetryCount: 3,
            FailedAt: DateTime.UtcNow,
            Payload: "{\"Data\":\"test\"}"
        );
}
