namespace Whycespace.Runtime.Versioning;

public sealed class UpgradeCoordinator
{
    private readonly RuntimeVersionManager _versionManager;
    private readonly List<UpgradeRecord> _history = new();

    public UpgradeCoordinator(RuntimeVersionManager versionManager)
    {
        _versionManager = versionManager;
    }

    public UpgradeRecord? PlanUpgrade(string componentId, string targetVersion)
    {
        var currentVersion = _versionManager.GetVersion(componentId);
        if (currentVersion is null)
            return null;

        if (!CompatibilityMatrix.IsUpgradeRequired(currentVersion, targetVersion))
            return null;

        var record = new UpgradeRecord(
            ComponentId: componentId,
            FromVersion: currentVersion,
            ToVersion: targetVersion,
            PlannedAt: DateTimeOffset.UtcNow);

        return record;
    }

    public void ApplyUpgrade(UpgradeRecord record)
    {
        _versionManager.RegisterVersion(record.ComponentId, record.ToVersion);
        _history.Add(record with { AppliedAt = DateTimeOffset.UtcNow });
    }

    public IReadOnlyList<UpgradeRecord> GetHistory() => _history.AsReadOnly();
}

public sealed record UpgradeRecord(
    string ComponentId,
    string FromVersion,
    string ToVersion,
    DateTimeOffset PlannedAt,
    DateTimeOffset? AppliedAt = null
);
