namespace Whycespace.Engines.T3I.Atlas.Economic.Models;

public sealed record ComputeCapitalBalanceCommand(
    Guid PoolId,
    Guid? InvestorIdentityId,
    string Currency,
    Guid RequestedBy,
    DateTime RequestedAt);
