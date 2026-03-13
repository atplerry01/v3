using Whycespace.Projections.Contracts;

namespace Whycespace.Projections.Registry;

public interface IProjectionRegistry
{
    void Register(IProjection projection);

    IReadOnlyCollection<IProjection> Resolve(string eventType);

    IReadOnlyCollection<IProjection> GetAll();
}
