namespace Whycespace.Runtime.EventSchemaRegistry.Models;

public enum CompatibilityLevel
{
    FullyCompatible,
    BackwardCompatible,
    Incompatible
}

public sealed record EventSchemaCompatibility(
    string EventId,
    EventSchemaVersion OldVersion,
    EventSchemaVersion NewVersion,
    CompatibilityLevel Level,
    IReadOnlyList<string> Issues
)
{
    public bool IsCompatible => Level != CompatibilityLevel.Incompatible;

    public static EventSchemaCompatibility Check(EventDescriptor older, EventDescriptor newer)
    {
        var issues = new List<string>();

        var oldProperties = older.Properties.ToDictionary(p => p.Name);
        var newProperties = newer.Properties.ToDictionary(p => p.Name);

        foreach (var oldProp in older.Properties)
        {
            if (!newProperties.TryGetValue(oldProp.Name, out var newProp))
            {
                issues.Add($"Removed property '{oldProp.Name}'.");
                continue;
            }

            if (oldProp.TypeName != newProp.TypeName)
            {
                issues.Add($"Property '{oldProp.Name}' type changed from '{oldProp.TypeName}' to '{newProp.TypeName}'.");
            }
        }

        var addedRequired = newer.Properties
            .Where(p => p.IsRequired && !oldProperties.ContainsKey(p.Name))
            .ToList();

        foreach (var added in addedRequired)
        {
            issues.Add($"Added required property '{added.Name}'.");
        }

        var level = issues.Count == 0
            ? CompatibilityLevel.FullyCompatible
            : issues.All(i => !i.Contains("Removed") && !i.Contains("type changed") && !i.Contains("required"))
                ? CompatibilityLevel.BackwardCompatible
                : CompatibilityLevel.Incompatible;

        return new EventSchemaCompatibility(
            newer.EventId,
            older.Version,
            newer.Version,
            level,
            issues
        );
    }
}
