using Whycespace.EventSchema.Models;

namespace Whycespace.EventSchema.Registry;

public sealed class EventSchemaRegistry
{
    private readonly Dictionary<string, List<Models.EventSchema>> _schemas = new();

    public void RegisterSchema(Models.EventSchema schema)
    {
        if (!_schemas.TryGetValue(schema.EventType, out var versions))
        {
            versions = new List<Models.EventSchema>();
            _schemas[schema.EventType] = versions;
        }

        versions.Add(schema);
    }

    public Models.EventSchema? GetLatest(string eventType)
    {
        if (!_schemas.TryGetValue(eventType, out var versions) || versions.Count == 0)
            return null;

        return versions[^1];
    }

    public Models.EventSchema? GetSchema(string eventType, int version)
    {
        if (!_schemas.TryGetValue(eventType, out var versions))
            return null;

        return versions.FirstOrDefault(s => s.SchemaVersion == version);
    }

    public IReadOnlyList<Models.EventSchema> GetAll()
    {
        return _schemas.Values.SelectMany(v => v).ToList();
    }
}
