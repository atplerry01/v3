namespace Whycespace.Contracts.Primitives;

public readonly record struct GuidId(Guid Value)
{
    public static GuidId New() => new(Guid.NewGuid());
    public static GuidId Empty => new(Guid.Empty);
    public static implicit operator Guid(GuidId id) => id.Value;
    public static implicit operator GuidId(Guid guid) => new(guid);
    public override string ToString() => Value.ToString();
}
