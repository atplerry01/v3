
using Whycespace.Shared.Primitives.Common;
using Whycespace.Shared.Envelopes;
using Whycespace.Contracts.Events;
using Whycespace.EventReplay.Engine;
using Whycespace.Reliability.Models;
using Whycespace.Reliability.State;

namespace Whycespace.EventReplay.Workflows;

public sealed class WorkflowStateRebuilder
{
    private readonly EventReplayEngine _engine;
    private readonly IWorkflowStateStore _stateStore;

    public WorkflowStateRebuilder(
        EventReplayEngine engine,
        IWorkflowStateStore stateStore)
    {
        _engine = engine;
        _stateStore = stateStore;
    }

    public async Task RebuildWorkflowsAsync(
        IReadOnlyList<EventEnvelope> workflowEvents,
        CancellationToken cancellationToken = default)
    {
        foreach (var envelope in workflowEvents)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (envelope.EventType == "WorkflowStarted")
            {
                var entry = new WorkflowStateEntry(
                    envelope.EventId,
                    envelope.EventType,
                    0,
                    envelope.PartitionKey,
                    new Dictionary<string, object>()
                );

                await _stateStore.SaveAsync(entry);
            }
        }

        await _engine.ReplayEventsAsync(workflowEvents, cancellationToken);
    }
}
