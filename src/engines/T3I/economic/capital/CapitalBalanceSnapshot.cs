namespace Whycespace.Engines.T3I.Economic.Capital;

public sealed record CapitalBalanceSnapshot(
    Guid PoolId,
    Guid? InvestorIdentityId,
    string Currency,
    decimal TotalCommittedCapital,
    decimal TotalContributedCapital,
    decimal TotalReservedCapital,
    decimal TotalAllocatedCapital,
    decimal TotalUtilizedCapital,
    decimal TotalDistributedCapital,
    decimal AvailableCapital,
    DateTime Timestamp);
