namespace Whycespace.System.Downstream.Economic.Vault.Participants;

public sealed record VaultParticipantRegistryRecord(
    Guid ParticipantId,
    Guid VaultId,
    Guid IdentityId,
    string ParticipantRole,
    string ParticipantStatus,
    decimal OwnershipPercentage,
    DateTime AddedAt,
    DateTime UpdatedAt,
    string? Description = null,
    string? GovernanceScope = null);
