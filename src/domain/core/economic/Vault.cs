namespace Whycespace.Domain.Core.Economic;

public sealed record Vault(
    Guid VaultId,
    Guid OwnerId,
    decimal Balance,
    string Currency,
    DateTimeOffset CreatedAt
);
