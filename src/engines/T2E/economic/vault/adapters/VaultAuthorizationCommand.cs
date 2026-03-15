namespace Whycespace.Engines.T2E.Economic.Vault.Adapters;

public sealed record VaultAuthorizationCommand(
    Guid IdentityId,
    Guid VaultId,
    Guid VaultAccountId,
    string OperationType,
    string ParticipantRole,
    DateTime RequestedAt,
    string? ReferenceId = null,
    string? ReferenceType = null);