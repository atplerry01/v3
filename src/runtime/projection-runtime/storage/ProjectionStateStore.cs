namespace Whycespace.ProjectionRuntime.Storage;

using Whycespace.ProjectionRuntime.Models;

public sealed class ProjectionStateStore
{
    private readonly Dictionary<string, ProjectionRecord> _records = new();

    public void Save(ProjectionRecord record)
    {
        var key = record.ProjectionName + ":" + record.EntityId;

        _records[key] = record;
    }

    public ProjectionRecord? Get(string projectionName, string entityId)
    {
        var key = projectionName + ":" + entityId;

        if (_records.TryGetValue(key, out var record))
            return record;

        return null;
    }

    public IReadOnlyCollection<ProjectionRecord> GetAll()
    {
        return _records.Values.ToList().AsReadOnly();
    }
}
