namespace Whycespace.Domain.Core.Economic;

public sealed record Capital(
    Guid CapitalId,
    Guid VaultId,
    decimal Amount,
    string Purpose,
    DateTimeOffset AllocatedAt
);
