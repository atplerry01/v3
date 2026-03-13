namespace Whycespace.Shared.Projections;

using Whycespace.Contracts.Events;

public interface IProjection
{
    string Name { get; }
    Task HandleAsync(SystemEvent @event);
}
