namespace Whycespace.Domain.Core.Vault;

public readonly record struct VaultId(Guid Value)
{
    public static VaultId New() => new(Guid.NewGuid());
    public static VaultId Empty => new(Guid.Empty);
    public static implicit operator Guid(VaultId id) => id.Value;
    public static implicit operator VaultId(Guid guid) => new(guid);
    public override string ToString() => Value.ToString();
}
