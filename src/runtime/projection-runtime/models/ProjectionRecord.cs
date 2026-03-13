namespace Whycespace.ProjectionRuntime.Models;

public sealed class ProjectionRecord
{
    public string ProjectionName { get; }

    public string EntityId { get; }

    public object State { get; }

    public DateTime UpdatedUtc { get; }

    public ProjectionRecord(string projectionName, string entityId, object state)
    {
        ProjectionName = projectionName;
        EntityId = entityId;
        State = state;
        UpdatedUtc = DateTime.UtcNow;
    }
}
