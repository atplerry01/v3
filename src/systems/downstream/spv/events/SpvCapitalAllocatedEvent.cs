using Whycespace.Contracts.Events;

namespace Whycespace.Systems.Downstream.Spv.Events;

public sealed record SpvCapitalAllocatedEvent(
    Guid EventId,
    string EventType,
    Guid AggregateId,
    DateTimeOffset Timestamp,
    Guid InvestorIdentityId,
    decimal AllocationPercentage,
    decimal InvestedAmount,
    string AllocationClass
) : EventBase(EventId, EventType, AggregateId, Timestamp)
{
    public static SpvCapitalAllocatedEvent Create(Guid spvId, Guid investorId, decimal percentage, decimal amount, string allocationClass) => new(
        Guid.NewGuid(), "SpvCapitalAllocatedEvent", spvId, DateTimeOffset.UtcNow,
        investorId, percentage, amount, allocationClass);
}
