namespace Whycespace.Contracts.Primitives;

public readonly record struct Timestamp(DateTimeOffset Value)
{
    public static Timestamp Now() => new(DateTimeOffset.UtcNow);
    public static implicit operator DateTimeOffset(Timestamp ts) => ts.Value;
    public static implicit operator Timestamp(DateTimeOffset dto) => new(dto);
    public override string ToString() => Value.ToString("O");
}
