using Whycespace.Contracts.Events;
using Whycespace.Contracts.Runtime;

namespace Whycespace.Systems.Downstream.Cwg.Events;

public sealed class CwgEventAdapter
{
    private readonly IEventBus _eventBus;

    public CwgEventAdapter(IEventBus eventBus)
    {
        _eventBus = eventBus;
    }

    public async Task PublishContributionReceivedAsync(Guid contributionId, Guid participantId, Guid vaultId, decimal amount, string contributionType)
    {
        var @event = SystemEvent.Create("ContributionReceivedEvent", contributionId, new Dictionary<string, object>
        {
            ["participantId"] = participantId.ToString(),
            ["vaultId"] = vaultId.ToString(),
            ["amount"] = amount,
            ["contributionType"] = contributionType
        });
        await _eventBus.PublishAsync(@event);
    }

    public async Task PublishVaultAllocationCompletedAsync(Guid allocationId, Guid vaultId, Guid recipientId, decimal percentage, string allocationType)
    {
        var @event = SystemEvent.Create("VaultAllocationCompletedEvent", allocationId, new Dictionary<string, object>
        {
            ["vaultId"] = vaultId.ToString(),
            ["recipientIdentityId"] = recipientId.ToString(),
            ["allocationPercentage"] = percentage,
            ["allocationType"] = allocationType
        });
        await _eventBus.PublishAsync(@event);
    }

    public async Task PublishExecutionStartedAsync(Guid aggregateId, string operationType, Guid initiatorId)
    {
        var @event = SystemEvent.Create("CwgExecutionStartedEvent", aggregateId, new Dictionary<string, object>
        {
            ["operationType"] = operationType,
            ["initiatorId"] = initiatorId.ToString()
        });
        await _eventBus.PublishAsync(@event);
    }

    public async Task PublishExecutionFailedAsync(Guid aggregateId, string operationType, string error)
    {
        var @event = SystemEvent.Create("CwgExecutionFailedEvent", aggregateId, new Dictionary<string, object>
        {
            ["operationType"] = operationType,
            ["errorMessage"] = error
        });
        await _eventBus.PublishAsync(@event);
    }
}
