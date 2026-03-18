namespace Whycespace.Contracts.Events;

public sealed record EventVersion(
    string EventType,
    int Major,
    int Minor
)
{
    public bool IsCompatibleWith(EventVersion other)
        => EventType == other.EventType && Major == other.Major;

    public override string ToString() => $"{EventType}:v{Major}.{Minor}";
}
