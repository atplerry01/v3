
using Whycespace.Shared.Primitives.Common;
using Whycespace.Shared.Envelopes;
using Whycespace.Contracts.Events;
using Whycespace.Shared.Primitives.Common;
using Whycespace.EventFabric.Router;
using Whycespace.EventFabric.Topics;
using Whycespace.EventReplay.Engine;
using Whycespace.EventReplay.Workflows;
using Whycespace.Reliability.State;

namespace Whycespace.EventReplay.Tests;

public class WorkflowStateRebuilderTests
{
    [Fact]
    public async Task RebuildWorkflowsAsync_Persists_WorkflowStarted_Events()
    {
        var router = new EventRouter();
        var engine = new EventReplayEngine(router);
        var store = new InMemoryWorkflowStateStore();
        var rebuilder = new WorkflowStateRebuilder(engine, store);

        var workflowEvents = new List<EventEnvelope>
        {
            new(Guid.NewGuid(), "WorkflowStarted", EventTopics.WorkflowEvents, new { },
                new PartitionKey("pk-1"), Timestamp.Now()),
            new(Guid.NewGuid(), "WorkflowStarted", EventTopics.WorkflowEvents, new { },
                new PartitionKey("pk-2"), Timestamp.Now()),
            new(Guid.NewGuid(), "WorkflowCompleted", EventTopics.WorkflowEvents, new { },
                new PartitionKey("pk-1"), Timestamp.Now())
        };

        await rebuilder.RebuildWorkflowsAsync(workflowEvents);

        Assert.Equal(2, store.Count);
    }

    [Fact]
    public async Task RebuildWorkflowsAsync_Replays_All_Events_Through_Router()
    {
        var routed = new List<EventEnvelope>();
        var router = new EventRouter();
        router.Register(EventTopics.WorkflowEvents, e => { routed.Add(e); return Task.CompletedTask; });

        var engine = new EventReplayEngine(router);
        var store = new InMemoryWorkflowStateStore();
        var rebuilder = new WorkflowStateRebuilder(engine, store);

        var workflowEvents = new List<EventEnvelope>
        {
            new(Guid.NewGuid(), "WorkflowStarted", EventTopics.WorkflowEvents, new { },
                new PartitionKey("pk-1"), Timestamp.Now()),
            new(Guid.NewGuid(), "StepCompleted", EventTopics.WorkflowEvents, new { },
                new PartitionKey("pk-1"), Timestamp.Now())
        };

        await rebuilder.RebuildWorkflowsAsync(workflowEvents);

        Assert.Equal(2, routed.Count);
    }
}
