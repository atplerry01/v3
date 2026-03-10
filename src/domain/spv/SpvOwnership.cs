namespace Whycespace.Domain.Spv;

public sealed record SpvOwnership(
    Guid OwnershipId,
    Guid SpvId,
    Guid OwnerId,
    decimal SharePercentage,
    OwnershipType Type,
    DateTimeOffset AcquiredAt
);

public enum OwnershipType
{
    Founder,
    Investor,
    Operator,
    Beneficiary
}
