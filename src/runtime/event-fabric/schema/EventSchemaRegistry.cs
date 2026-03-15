namespace Whycespace.EventFabric.Schema;

public sealed class EventSchemaRegistry
{
    private readonly Dictionary<string, List<EventSchemaDefinition>> _schemas = new();

    public void Register(EventSchemaDefinition schema)
    {
        if (!_schemas.TryGetValue(schema.EventType, out var versions))
        {
            versions = new List<EventSchemaDefinition>();
            _schemas[schema.EventType] = versions;
        }

        if (versions.Any(v => v.Version == schema.Version))
            throw new InvalidOperationException(
                $"Schema version {schema.Version} for '{schema.EventType}' is already registered.");

        versions.Add(schema);
    }

    public EventSchemaDefinition? GetLatest(string eventType)
    {
        if (!_schemas.TryGetValue(eventType, out var versions) || versions.Count == 0)
            return null;

        return versions[^1];
    }

    public EventSchemaDefinition? GetSchema(string eventType, int version)
    {
        if (!_schemas.TryGetValue(eventType, out var versions))
            return null;

        return versions.FirstOrDefault(s => s.Version == version);
    }

    public bool HasSchema(string eventType)
    {
        return _schemas.ContainsKey(eventType) && _schemas[eventType].Count > 0;
    }

    public IReadOnlyList<EventSchemaDefinition> GetAll()
    {
        return _schemas.Values.SelectMany(v => v).ToList().AsReadOnly();
    }
}
