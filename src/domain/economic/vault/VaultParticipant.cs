namespace Whycespace.Domain.Core.Economic;

public sealed class VaultParticipant
{
    public VaultParticipantId ParticipantId { get; }
    public Guid VaultId { get; }
    public Guid IdentityId { get; }
    public VaultParticipantRole Role { get; private set; }
    public VaultParticipantStatus Status { get; private set; }
    public DateTimeOffset AddedAt { get; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public decimal OwnershipPercentage { get; private set; }
    public string? Description { get; private set; }

    public VaultParticipant(
        Guid vaultId,
        Guid identityId,
        VaultParticipantRole role,
        decimal ownershipPercentage = 0m,
        string? description = null)
    {
        if (vaultId == Guid.Empty)
            throw new InvalidOperationException("VaultId must be specified.");

        if (identityId == Guid.Empty)
            throw new InvalidOperationException("IdentityId must be specified.");

        if (ownershipPercentage < 0 || ownershipPercentage > 100)
            throw new InvalidOperationException("Ownership percentage must be between 0 and 100.");

        ParticipantId = VaultParticipantId.New();
        VaultId = vaultId;
        IdentityId = identityId;
        Role = role;
        Status = VaultParticipantStatus.Active;
        OwnershipPercentage = ownershipPercentage;
        Description = description;
        AddedAt = DateTimeOffset.UtcNow;
        UpdatedAt = AddedAt;
    }

    public bool IsActiveParticipant() => Status == VaultParticipantStatus.Active;

    public bool IsOwner() => Role == VaultParticipantRole.Owner;

    public void SuspendParticipant()
    {
        if (Status == VaultParticipantStatus.Removed)
            throw new InvalidOperationException("Removed participant cannot be suspended.");

        if (Status == VaultParticipantStatus.Suspended)
            throw new InvalidOperationException("Participant is already suspended.");

        Status = VaultParticipantStatus.Suspended;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void RemoveParticipant(bool isLastOwner)
    {
        if (IsOwner() && isLastOwner)
            throw new InvalidOperationException("Last owner cannot be removed from the vault.");

        if (Status == VaultParticipantStatus.Removed)
            throw new InvalidOperationException("Participant is already removed.");

        Status = VaultParticipantStatus.Removed;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void UpdateRole(VaultParticipantRole newRole)
    {
        Role = newRole;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void UpdateOwnershipPercentage(decimal percentage)
    {
        if (percentage < 0 || percentage > 100)
            throw new InvalidOperationException("Ownership percentage must be between 0 and 100.");

        OwnershipPercentage = percentage;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
