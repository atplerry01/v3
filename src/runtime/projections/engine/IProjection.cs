using Whycespace.EventFabric.Models;

namespace Whycespace.Projections.Engine;

public interface IProjection
{
    string Name { get; }

    IReadOnlyCollection<string> EventTypes { get; }

    Task HandleAsync(EventEnvelope envelope);
}
