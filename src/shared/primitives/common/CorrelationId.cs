namespace Whycespace.Shared.Primitives.Common;

public readonly record struct CorrelationId(string Value)
{
    public static CorrelationId New() => new(Guid.NewGuid().ToString("N"));
    public static CorrelationId Empty => new("");

    public bool IsEmpty => string.IsNullOrWhiteSpace(Value);

    public static implicit operator string(CorrelationId id) => id.Value;
    public static implicit operator CorrelationId(string value) => new(value);

    public override string ToString() => Value;
}
