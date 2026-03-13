namespace Whycespace.EngineManifest.Models;

public sealed class EngineCapability
{
    public string Name { get; }

    public EngineCapability(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Capability name is required.", nameof(name));

        Name = name;
    }

    public override bool Equals(object? obj) =>
        obj is EngineCapability other && Name == other.Name;

    public override int GetHashCode() => Name.GetHashCode();

    public override string ToString() => Name;
}
