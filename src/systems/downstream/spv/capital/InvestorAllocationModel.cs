namespace Whycespace.Systems.Downstream.Spv.Capital;

public sealed record InvestorAllocationModel(
    Guid AllocationId,
    Guid InvestorIdentityId,
    decimal AllocationPercentage,
    decimal InvestedAmount,
    string AllocationClass,
    DateTimeOffset AllocatedAt,
    string? Notes = null
);
