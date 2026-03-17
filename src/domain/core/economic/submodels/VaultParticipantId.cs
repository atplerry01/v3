namespace Whycespace.Domain.Core.Vault;

public readonly record struct VaultParticipantId(Guid Value)
{
    public static VaultParticipantId New() => new(Guid.NewGuid());
    public static VaultParticipantId Empty => new(Guid.Empty);
    public static implicit operator Guid(VaultParticipantId id) => id.Value;
    public static implicit operator VaultParticipantId(Guid guid) => new(guid);
    public override string ToString() => Value.ToString();
}
