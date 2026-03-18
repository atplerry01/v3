namespace Whycespace.Domain.Core.Economic;

public readonly record struct VaultAllocationId(Guid Value)
{
    public static VaultAllocationId New() => new(Guid.NewGuid());
    public static VaultAllocationId Empty => new(Guid.Empty);
    public static implicit operator Guid(VaultAllocationId id) => id.Value;
    public static implicit operator VaultAllocationId(Guid guid) => new(guid);
    public override string ToString() => Value.ToString();
}
