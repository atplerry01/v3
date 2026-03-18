namespace Whycespace.Contracts.Tests;

using Whycespace.Contracts.Engines;

public sealed class EngineContextTests
{
    [Fact]
    public void EngineContext_Is_Immutable_Record()
    {
        var data = new Dictionary<string, object> { ["key"] = "value" };
        var context = new EngineContext(
            Guid.NewGuid(), "wf-1", "step-1", "partition-1", data);

        Assert.Equal("wf-1", context.WorkflowId);
        Assert.Equal("step-1", context.WorkflowStep);
        Assert.Equal("partition-1", context.PartitionKey);
        Assert.Equal("value", context.Data["key"]);
    }

    [Fact]
    public void EngineResult_Ok_Creates_Success()
    {
        var events = new[] { EngineEvent.Create("Test", Guid.NewGuid()) };
        var result = EngineResult.Ok(events);

        Assert.True(result.Success);
        Assert.Single(result.Events);
    }

    [Fact]
    public void EngineResult_Fail_Creates_Failure()
    {
        var result = EngineResult.Fail("something went wrong");

        Assert.False(result.Success);
        Assert.Empty(result.Events);
        Assert.Equal("something went wrong", result.Output["error"]);
    }

    [Fact]
    public void EngineInvocationEnvelope_Is_Immutable()
    {
        var envelope = new EngineInvocationEnvelope(
            Guid.NewGuid(), "RideExecution", "wf-1",
            "step-1", "partition-1", new Dictionary<string, object>());

        Assert.Equal("RideExecution", envelope.EngineName);
        Assert.Equal("wf-1", envelope.WorkflowId);
    }

    [Fact]
    public void EngineEvent_Create_Generates_Valid_Event()
    {
        var aggregateId = Guid.NewGuid();
        var @event = EngineEvent.Create("RideRequested", aggregateId);

        Assert.NotEqual(Guid.Empty, @event.EventId);
        Assert.Equal("RideRequested", @event.EventType);
        Assert.Equal(aggregateId, @event.AggregateId);
    }
}
