namespace Whycespace.ProjectionRuntime.Workers;

using Whycespace.ProjectionRuntime.Runtime;

public sealed class ProjectionWorker
{
    private readonly ProjectionEngine _engine;

    public ProjectionWorker(ProjectionEngine engine)
    {
        _engine = engine;
    }

    public void Handle(string eventType, string entityId, object state)
    {
        _engine.Apply(eventType, entityId, state);
    }
}
