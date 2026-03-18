namespace Whycespace.Domain.Economic.Vault;

public readonly record struct VaultTransactionId(Guid Value)
{
    public static VaultTransactionId New() => new(Guid.NewGuid());
    public static VaultTransactionId Empty => new(Guid.Empty);
    public static implicit operator Guid(VaultTransactionId id) => id.Value;
    public static implicit operator VaultTransactionId(Guid guid) => new(guid);
    public override string ToString() => Value.ToString();
}
