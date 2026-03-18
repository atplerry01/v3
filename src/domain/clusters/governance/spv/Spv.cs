namespace Whycespace.Domain.Core.Spv;

public sealed record Spv(
    Guid SpvId,
    string Name,
    Guid CapitalId,
    decimal AllocatedCapital,
    SpvStatus Status,
    DateTimeOffset CreatedAt
);

public enum SpvStatus
{
    Active,
    Suspended,
    Dissolved
}
