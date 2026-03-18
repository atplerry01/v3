using Whycespace.Contracts.Events;
using Whycespace.Contracts.Runtime;

namespace Whycespace.Systems.Downstream.Work.Events;

public sealed class WorkEventAdapter
{
    private readonly IEventBus _eventBus;

    public WorkEventAdapter(IEventBus eventBus)
    {
        _eventBus = eventBus;
    }

    public async Task PublishExecutionStartedAsync(Guid taskId, string clusterId, string subClusterId, string workerId, string taskType)
    {
        var @event = SystemEvent.Create("WorkExecutionStartedEvent", taskId, new Dictionary<string, object>
        {
            ["clusterId"] = clusterId,
            ["subClusterId"] = subClusterId,
            ["workerId"] = workerId,
            ["taskType"] = taskType
        });
        await _eventBus.PublishAsync(@event);
    }

    public async Task PublishExecutionCompletedAsync(Guid taskId, string clusterId, string workerId, string taskType, string result)
    {
        var @event = SystemEvent.Create("WorkExecutionCompletedEvent", taskId, new Dictionary<string, object>
        {
            ["clusterId"] = clusterId,
            ["workerId"] = workerId,
            ["taskType"] = taskType,
            ["result"] = result
        });
        await _eventBus.PublishAsync(@event);
    }

    public async Task PublishExecutionFailedAsync(Guid taskId, string clusterId, string workerId, string taskType, string error)
    {
        var @event = SystemEvent.Create("WorkExecutionFailedEvent", taskId, new Dictionary<string, object>
        {
            ["clusterId"] = clusterId,
            ["workerId"] = workerId,
            ["taskType"] = taskType,
            ["errorMessage"] = error
        });
        await _eventBus.PublishAsync(@event);
    }

    public async Task PublishCommandReceivedAsync(Guid commandId, string commandType, string clusterId, string initiatorId)
    {
        var @event = SystemEvent.Create("WorkCommandReceivedEvent", commandId, new Dictionary<string, object>
        {
            ["commandType"] = commandType,
            ["clusterId"] = clusterId,
            ["initiatorId"] = initiatorId
        });
        await _eventBus.PublishAsync(@event);
    }
}
