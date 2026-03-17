namespace Whycespace.Runtime.EventSchemaRegistry.Models;

public sealed record EventSchemaVersion(int Major, int Minor = 0)
    : IComparable<EventSchemaVersion>
{
    public int CompareTo(EventSchemaVersion? other)
    {
        if (other is null) return 1;
        var majorCompare = Major.CompareTo(other.Major);
        return majorCompare != 0 ? majorCompare : Minor.CompareTo(other.Minor);
    }

    public bool IsCompatibleWith(EventSchemaVersion other)
        => Major == other.Major;

    public override string ToString() => $"{Major}.{Minor}";

    public static EventSchemaVersion Parse(string value)
    {
        var parts = value.Split('.');
        var major = int.Parse(parts[0]);
        var minor = parts.Length > 1 ? int.Parse(parts[1]) : 0;
        return new EventSchemaVersion(major, minor);
    }
}
