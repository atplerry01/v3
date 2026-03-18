
using Whycespace.Contracts.Events;
using Whycespace.Shared.Envelopes;

namespace Whycespace.ProjectionRuntime.Projections.Contracts;

public interface IProjection
{
    string Name { get; }

    IReadOnlyCollection<string> EventTypes { get; }

    Task HandleAsync(EventEnvelope envelope);
}
