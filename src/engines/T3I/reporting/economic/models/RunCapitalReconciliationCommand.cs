namespace Whycespace.Engines.T3I.Reporting.Economic.Models;

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
