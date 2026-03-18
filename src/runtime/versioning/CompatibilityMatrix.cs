namespace Whycespace.Runtime.Versioning;

public static class CompatibilityMatrix
{
    public static bool IsCompatible(string currentVersion, string requiredVersion)
    {
        var current = ParseVersion(currentVersion);
        var required = ParseVersion(requiredVersion);

        // Same major version = compatible (semver)
        return current.Major == required.Major && current.Minor >= required.Minor;
    }

    public static bool IsUpgradeRequired(string currentVersion, string targetVersion)
    {
        var current = ParseVersion(currentVersion);
        var target = ParseVersion(targetVersion);

        return target.Major > current.Major ||
               (target.Major == current.Major && target.Minor > current.Minor);
    }

    private static (int Major, int Minor, int Patch) ParseVersion(string version)
    {
        var parts = version.Split('.');
        return (
            parts.Length > 0 && int.TryParse(parts[0], out var major) ? major : 0,
            parts.Length > 1 && int.TryParse(parts[1], out var minor) ? minor : 0,
            parts.Length > 2 && int.TryParse(parts[2], out var patch) ? patch : 0
        );
    }
}
