namespace Whycespace.Contracts.Primitives;

public readonly record struct PartitionKey(string Value)
{
    public static PartitionKey Empty => new("");

    public bool IsEmpty => string.IsNullOrWhiteSpace(Value);

    public static implicit operator string(PartitionKey key) => key.Value;
    public static implicit operator PartitionKey(string value) => new(value);

    public override string ToString() => Value;
}
