namespace Whycespace.Runtime.CommandRegistry;

public sealed record CommandVersion(int Major, int Minor = 0) : IComparable<CommandVersion>
{
    public int CompareTo(CommandVersion? other)
    {
        if (other is null) return 1;

        var majorComparison = Major.CompareTo(other.Major);
        return majorComparison != 0 ? majorComparison : Minor.CompareTo(other.Minor);
    }

    public bool IsCompatibleWith(CommandVersion other)
    {
        ArgumentNullException.ThrowIfNull(other);
        return Major == other.Major;
    }

    public override string ToString() => $"{Major}.{Minor}";

    public static CommandVersion Parse(string version)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(version);

        var parts = version.Split('.');
        if (parts.Length < 1 || parts.Length > 2)
            throw new FormatException($"Invalid command version format: '{version}'. Expected 'Major' or 'Major.Minor'.");

        if (!int.TryParse(parts[0], out var major))
            throw new FormatException($"Invalid major version: '{parts[0]}'.");

        var minor = 0;
        if (parts.Length == 2 && !int.TryParse(parts[1], out minor))
            throw new FormatException($"Invalid minor version: '{parts[1]}'.");

        return new CommandVersion(major, minor);
    }
}
