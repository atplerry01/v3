using Whycespace.Shared.Envelopes;

namespace Whycespace.Shared.Projections;

public interface IProjection
{
    string Name { get; }
    Task HandleAsync(EventEnvelope envelope);
}
