namespace Whycespace.Engines.T2E.Economic.Vault.Models;

public sealed record ValidateVaultGovernanceCommand(
    Guid VaultId,
    Guid VaultAccountId,
    Guid InitiatorIdentityId,
    string OperationType,
    decimal Amount,
    string GovernanceScope,
    DateTime RequestedAt,
    string? ReferenceId = null,
    string? ReferenceType = null);
