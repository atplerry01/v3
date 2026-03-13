namespace Whycespace.ProjectionRuntime.Storage;

using Whycespace.ProjectionRuntime.Models;

/// <summary>
/// Thread Safety Notice
/// --------------------
/// This component is designed for single-threaded runtime access.
///
/// In the Whycespace runtime architecture, execution is serialized
/// through partition workers and workflow dispatchers.
///
/// Because of this guarantee, concurrent collections are not required
/// and standard Dictionary/List structures are used for efficiency.
///
/// If this component is used outside the partition runtime context,
/// external synchronization must be applied.
/// </summary>
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
