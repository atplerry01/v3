using Whycespace.ProjectionRuntime.Projections.Contracts;

namespace Whycespace.ProjectionRuntime.Projections.Registry;

public interface IProjectionRegistry
{
    void Register(IProjection projection);

    IReadOnlyCollection<IProjection> Resolve(string eventType);

    IReadOnlyCollection<IProjection> GetAll();
}
