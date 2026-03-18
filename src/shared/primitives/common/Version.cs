namespace Whycespace.Shared.Primitives.Common;

public readonly record struct Version(int Major, int Minor, int Patch)
{
    public static Version Initial => new(1, 0, 0);

    public Version IncrementMajor() => new(Major + 1, 0, 0);
    public Version IncrementMinor() => new(Major, Minor + 1, 0);
    public Version IncrementPatch() => new(Major, Minor, Patch + 1);

    public override string ToString() => $"{Major}.{Minor}.{Patch}";
}
