namespace Whycespace.Engines.T2E.Economic.Vault.Purpose.Models;

public sealed record ValidateVaultPurposeCommand(
    Guid VaultId,
    string VaultPurpose,
    string TransactionType,
    decimal Amount,
    Guid InitiatorIdentityId,
    DateTime RequestedAt,
    string? ReferenceId = null,
    string? ReferenceType = null);
