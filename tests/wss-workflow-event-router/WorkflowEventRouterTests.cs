using Whycespace.Engines.T1M.WSS.Runtime;
using Whycespace.Runtime.Events;
using Whycespace.Systems.Midstream.WSS.Events;
using Whycespace.Systems.Midstream.WSS.Kafka;

namespace Whycespace.WSS.WorkflowEventRouter.Tests;

public class WorkflowEventRouterTests
{
    private readonly Whycespace.Engines.T1M.WSS.Runtime.WorkflowEventRouter _router;
    private readonly EventBus _eventBus;

    public WorkflowEventRouterTests()
    {
        _eventBus = new EventBus();
        var kafkaPublisher = new KafkaEventPublisher(_eventBus, "localhost:9092");
        _router = new Whycespace.Engines.T1M.WSS.Runtime.WorkflowEventRouter(kafkaPublisher);
    }

    // 1. Publish workflow event
    [Fact]
    public async Task PublishEvent_ShouldCreateEnvelopeAndPublish()
    {
        var published = new List<WorkflowEventEnvelope>();
        _router.Subscribe(WorkflowEventTypes.WorkflowStarted, envelope =>
        {
            published.Add(envelope);
            return Task.CompletedTask;
        });

        await _router.PublishEvent(
            WorkflowEventTypes.WorkflowStarted,
            "taxi-request",
            "wf-inst-001",
            new Dictionary<string, object> { ["passenger"] = "user-123" });

        Assert.Single(published);
        Assert.Equal(WorkflowEventTypes.WorkflowStarted, published[0].EventType);
        Assert.Equal("taxi-request", published[0].WorkflowId);
        Assert.Equal("wf-inst-001", published[0].InstanceId);
        Assert.Equal("user-123", published[0].Payload["passenger"]);
    }

    // 2. Subscribe to workflow event
    [Fact]
    public async Task Subscribe_ShouldRegisterHandler()
    {
        var received = false;
        _router.Subscribe(WorkflowEventTypes.WorkflowCompleted, _ =>
        {
            received = true;
            return Task.CompletedTask;
        });

        var envelope = WorkflowEventEnvelope.Create(
            WorkflowEventTypes.WorkflowCompleted, "wf-1", "inst-1");

        await _router.RouteInternalEvent(envelope);

        Assert.True(received);
    }

    // 3. Route internal workflow event
    [Fact]
    public async Task RouteInternalEvent_ShouldDeliverToSubscribers()
    {
        var deliveredPayloads = new List<IDictionary<string, object>>();

        _router.Subscribe(WorkflowEventTypes.WorkflowStepCompleted, envelope =>
        {
            deliveredPayloads.Add(envelope.Payload);
            return Task.CompletedTask;
        });

        var envelope = WorkflowEventEnvelope.Create(
            WorkflowEventTypes.WorkflowStepCompleted,
            "ride-request",
            "inst-42",
            new Dictionary<string, object> { ["step"] = "find-driver" });

        await _router.RouteInternalEvent(envelope);

        Assert.Single(deliveredPayloads);
        Assert.Equal("find-driver", deliveredPayloads[0]["step"]);
    }

    // 4. Kafka publishing success
    [Fact]
    public async Task PublishEvent_ShouldPublishToEventBus()
    {
        await _router.PublishEvent(
            WorkflowEventTypes.WorkflowStepCompleted,
            "taxi-request",
            "wf-inst-002",
            new Dictionary<string, object> { ["step"] = "validate-passenger" });

        var busEvents = _eventBus.GetPublishedEvents();
        Assert.Single(busEvents);
        Assert.Equal(WorkflowEventTypes.WorkflowStepCompleted, busEvents[0].EventType);
    }

    // 5. Multiple subscribers handling
    [Fact]
    public async Task RouteInternalEvent_MultipleSubscribers_ShouldDeliverToAll()
    {
        var count = 0;

        _router.Subscribe(WorkflowEventTypes.WorkflowStarted, _ =>
        {
            Interlocked.Increment(ref count);
            return Task.CompletedTask;
        });

        _router.Subscribe(WorkflowEventTypes.WorkflowStarted, _ =>
        {
            Interlocked.Increment(ref count);
            return Task.CompletedTask;
        });

        _router.Subscribe(WorkflowEventTypes.WorkflowStarted, _ =>
        {
            Interlocked.Increment(ref count);
            return Task.CompletedTask;
        });

        var envelope = WorkflowEventEnvelope.Create(
            WorkflowEventTypes.WorkflowStarted, "wf-1", "inst-1");

        await _router.RouteInternalEvent(envelope);

        Assert.Equal(3, count);
    }

    // 6. Unknown event handling
    [Fact]
    public async Task RouteInternalEvent_UnknownEventType_ShouldNotThrow()
    {
        var envelope = WorkflowEventEnvelope.Create(
            "UnknownEventType", "wf-1", "inst-1");

        await _router.RouteInternalEvent(envelope);
    }
}
