namespace Whycespace.Engines.T3I.Reporting.Economic;

public enum DiscrepancyType
{
    BalanceMismatch,
    InvalidReservationAmount,
    AllocationOverflow,
    UtilizationExceedsAllocation,
    DistributionInconsistency
}

public sealed record DiscrepancyRecord(
    DiscrepancyType Type,
    Guid ReferenceId,
    decimal ExpectedValue,
    decimal ActualValue,
    string Description);

public sealed record CapitalReconciliationReport(
    Guid PoolId,
    Guid? InvestorIdentityId,
    ReconciliationScope Scope,
    IReadOnlyList<DiscrepancyRecord> Discrepancies,
    int TotalRecordsChecked,
    int TotalDiscrepancies,
    DateTime Timestamp)
{
    public static CapitalReconciliationReport Create(
        Guid poolId,
        Guid? investorIdentityId,
        ReconciliationScope scope,
        IReadOnlyList<DiscrepancyRecord> discrepancies,
        int totalRecordsChecked)
    {
        return new CapitalReconciliationReport(
            PoolId: poolId,
            InvestorIdentityId: investorIdentityId,
            Scope: scope,
            Discrepancies: discrepancies,
            TotalRecordsChecked: totalRecordsChecked,
            TotalDiscrepancies: discrepancies.Count,
            Timestamp: DateTime.UtcNow);
    }
}
