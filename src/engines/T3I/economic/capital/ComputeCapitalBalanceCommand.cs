namespace Whycespace.Engines.T3I.Economic.Capital;

public sealed record ComputeCapitalBalanceCommand(
    Guid PoolId,
    Guid? InvestorIdentityId,
    string Currency,
    Guid RequestedBy,
    DateTime RequestedAt);
