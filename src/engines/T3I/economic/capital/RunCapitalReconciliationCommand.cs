namespace Whycespace.Engines.T3I.Economic.Capital;

public enum ReconciliationScope
{
    PoolLevel,
    InvestorLevel,
    AllocationLevel,
    FullSystem
}

public sealed record RunCapitalReconciliationCommand(
    Guid PoolId,
    Guid? InvestorIdentityId,
    ReconciliationScope Scope,
    Guid RequestedBy,
    DateTime RequestedAt);
