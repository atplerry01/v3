namespace Whycespace.Engines.T2E.Economic.Vault.Adapters;

public sealed record VaultAuthorizationResult(
    Guid IdentityId,
    Guid VaultId,
    string OperationType,
    bool IsAuthorized,
    string AuthorizationReason,
    DateTime EvaluatedAt);