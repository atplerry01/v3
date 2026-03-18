namespace Whycespace.Domain.Core.Operators;

public readonly record struct OperatorId(Guid Value)
{
    public static OperatorId New() => new(Guid.NewGuid());
    public static OperatorId Empty => new(Guid.Empty);
    public static implicit operator Guid(OperatorId id) => id.Value;
    public static implicit operator OperatorId(Guid guid) => new(guid);
    public override string ToString() => Value.ToString();
}
