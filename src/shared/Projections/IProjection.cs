namespace Whycespace.Shared.Projections;

using Whycespace.Shared.Events;

public interface IProjection
{
    string Name { get; }
    Task HandleAsync(SystemEvent @event);
}
