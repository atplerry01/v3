namespace Whycespace.Runtime.Persistence.Abstractions;

using Whycespace.Contracts.Events;

public interface IEventStore
{
    Task InitializeAsync();
    Task AppendAsync(SystemEvent @event);
    Task<IReadOnlyList<SystemEvent>> GetByAggregateAsync(Guid aggregateId);
}
