using Whycespace.Contracts.Events;
using Whycespace.Contracts.Runtime;

namespace Whycespace.Systems.Downstream.Spv.Events;

public sealed class SpvEventAdapter
{
    private readonly IEventBus _eventBus;

    public SpvEventAdapter(IEventBus eventBus)
    {
        _eventBus = eventBus;
    }

    public async Task PublishSpvCreatedAsync(Guid spvId, string name, string clusterId, decimal allocatedCapital)
    {
        var @event = SystemEvent.Create("SpvCreatedEvent", spvId, new Dictionary<string, object>
        {
            ["name"] = name,
            ["clusterId"] = clusterId,
            ["allocatedCapital"] = allocatedCapital
        });
        await _eventBus.PublishAsync(@event);
    }

    public async Task PublishCapitalAllocatedAsync(Guid spvId, Guid investorId, decimal percentage, decimal amount, string allocationClass)
    {
        var @event = SystemEvent.Create("SpvCapitalAllocatedEvent", spvId, new Dictionary<string, object>
        {
            ["investorIdentityId"] = investorId.ToString(),
            ["allocationPercentage"] = percentage,
            ["investedAmount"] = amount,
            ["allocationClass"] = allocationClass
        });
        await _eventBus.PublishAsync(@event);
    }

    public async Task PublishLifecycleTransitionAsync(Guid spvId, string fromState, string toState, string reason)
    {
        var @event = SystemEvent.Create("SpvLifecycleTransitionEvent", spvId, new Dictionary<string, object>
        {
            ["fromState"] = fromState,
            ["toState"] = toState,
            ["reason"] = reason
        });
        await _eventBus.PublishAsync(@event);
    }
}
