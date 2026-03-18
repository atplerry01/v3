namespace Whycespace.Engines.T3I.Reporting.Economic.Models;

public sealed record ContributionRecord(
    Guid ContributionId,
    Guid PoolId,
    Guid InvestorIdentityId,
    decimal Amount,
    string Currency,
    DateTime Timestamp);

public sealed record LedgerEntryRecord(
    Guid EntryId,
    Guid PoolId,
    string EntryType,
    decimal Amount,
    Guid ReferenceId,
    DateTime Timestamp);

public sealed record ReservationRecord(
    Guid ReservationId,
    Guid PoolId,
    decimal ReservedAmount,
    decimal AllocatedAmount,
    bool IsActive);

public sealed record AllocationRecord(
    Guid AllocationId,
    Guid ReservationId,
    Guid PoolId,
    decimal AllocatedAmount,
    decimal UtilizedAmount,
    bool IsActive);

public sealed record DistributionRecord(
    Guid DistributionId,
    Guid PoolId,
    Guid InvestorIdentityId,
    decimal Amount,
    decimal OwnershipPercentage,
    DateTime Timestamp);

public sealed record PoolBalanceSummary(
    Guid PoolId,
    decimal TotalContributions,
    decimal TotalReserved,
    decimal TotalAllocated,
    decimal TotalUtilized,
    decimal TotalDistributed,
    decimal ReportedBalance);

public sealed class CapitalReconciliationEngine
{
    public CapitalReconciliationReport Reconcile(
        RunCapitalReconciliationCommand command,
        PoolBalanceSummary? poolSummary,
        IReadOnlyList<ContributionRecord> contributions,
        IReadOnlyList<LedgerEntryRecord> ledgerEntries,
        IReadOnlyList<ReservationRecord> reservations,
        IReadOnlyList<AllocationRecord> allocations,
        IReadOnlyList<DistributionRecord> distributions)
    {
        var discrepancies = new List<DiscrepancyRecord>();
        var totalRecordsChecked = 0;

        if (command.Scope is ReconciliationScope.PoolLevel or ReconciliationScope.FullSystem)
        {
            totalRecordsChecked += ReconcilePoolBalance(
                command, poolSummary, contributions, ledgerEntries, discrepancies);

            totalRecordsChecked += ReconcileReservations(
                command, poolSummary, reservations, discrepancies);
        }

        if (command.Scope is ReconciliationScope.AllocationLevel or ReconciliationScope.FullSystem)
        {
            totalRecordsChecked += ReconcileAllocations(
                command, reservations, allocations, discrepancies);
        }

        if (command.Scope is ReconciliationScope.InvestorLevel or ReconciliationScope.FullSystem)
        {
            totalRecordsChecked += ReconcileDistributions(
                command, distributions, discrepancies);
        }

        if (command.Scope == ReconciliationScope.FullSystem)
        {
            totalRecordsChecked += ReconcileContributionsAgainstLedger(
                contributions, ledgerEntries, discrepancies);
        }

        return CapitalReconciliationReport.Create(
            command.PoolId,
            command.InvestorIdentityId,
            command.Scope,
            discrepancies,
            totalRecordsChecked);
    }

    private static int ReconcilePoolBalance(
        RunCapitalReconciliationCommand command,
        PoolBalanceSummary? poolSummary,
        IReadOnlyList<ContributionRecord> contributions,
        IReadOnlyList<LedgerEntryRecord> ledgerEntries,
        List<DiscrepancyRecord> discrepancies)
    {
        if (poolSummary is null)
            return 0;

        var recordsChecked = 0;

        var computedContributions = contributions
            .Where(c => c.PoolId == command.PoolId)
            .Sum(c => c.Amount);

        recordsChecked += contributions.Count(c => c.PoolId == command.PoolId);

        if (computedContributions != poolSummary.TotalContributions)
        {
            discrepancies.Add(new DiscrepancyRecord(
                Type: DiscrepancyType.BalanceMismatch,
                ReferenceId: command.PoolId,
                ExpectedValue: computedContributions,
                ActualValue: poolSummary.TotalContributions,
                Description: $"Pool {command.PoolId}: computed contributions {computedContributions} " +
                             $"does not match reported contributions {poolSummary.TotalContributions}"));
        }

        var computedBalance = computedContributions
            - poolSummary.TotalReserved
            - poolSummary.TotalDistributed;

        if (computedBalance != poolSummary.ReportedBalance)
        {
            discrepancies.Add(new DiscrepancyRecord(
                Type: DiscrepancyType.BalanceMismatch,
                ReferenceId: command.PoolId,
                ExpectedValue: computedBalance,
                ActualValue: poolSummary.ReportedBalance,
                Description: $"Pool {command.PoolId}: computed balance {computedBalance} " +
                             $"does not match reported balance {poolSummary.ReportedBalance}"));
        }

        recordsChecked++;
        return recordsChecked;
    }

    private static int ReconcileReservations(
        RunCapitalReconciliationCommand command,
        PoolBalanceSummary? poolSummary,
        IReadOnlyList<ReservationRecord> reservations,
        List<DiscrepancyRecord> discrepancies)
    {
        if (poolSummary is null)
            return 0;

        var poolReservations = reservations
            .Where(r => r.PoolId == command.PoolId && r.IsActive)
            .ToList();

        var totalReserved = poolReservations.Sum(r => r.ReservedAmount);

        if (totalReserved != poolSummary.TotalReserved)
        {
            discrepancies.Add(new DiscrepancyRecord(
                Type: DiscrepancyType.InvalidReservationAmount,
                ReferenceId: command.PoolId,
                ExpectedValue: totalReserved,
                ActualValue: poolSummary.TotalReserved,
                Description: $"Pool {command.PoolId}: sum of active reservations {totalReserved} " +
                             $"does not match reported total reserved {poolSummary.TotalReserved}"));
        }

        foreach (var reservation in poolReservations)
        {
            if (reservation.AllocatedAmount > reservation.ReservedAmount)
            {
                discrepancies.Add(new DiscrepancyRecord(
                    Type: DiscrepancyType.AllocationOverflow,
                    ReferenceId: reservation.ReservationId,
                    ExpectedValue: reservation.ReservedAmount,
                    ActualValue: reservation.AllocatedAmount,
                    Description: $"Reservation {reservation.ReservationId}: allocated amount " +
                                 $"{reservation.AllocatedAmount} exceeds reserved amount {reservation.ReservedAmount}"));
            }
        }

        return poolReservations.Count + 1;
    }

    private static int ReconcileAllocations(
        RunCapitalReconciliationCommand command,
        IReadOnlyList<ReservationRecord> reservations,
        IReadOnlyList<AllocationRecord> allocations,
        List<DiscrepancyRecord> discrepancies)
    {
        var poolAllocations = allocations
            .Where(a => a.PoolId == command.PoolId && a.IsActive)
            .ToList();

        foreach (var allocation in poolAllocations)
        {
            if (allocation.UtilizedAmount > allocation.AllocatedAmount)
            {
                discrepancies.Add(new DiscrepancyRecord(
                    Type: DiscrepancyType.UtilizationExceedsAllocation,
                    ReferenceId: allocation.AllocationId,
                    ExpectedValue: allocation.AllocatedAmount,
                    ActualValue: allocation.UtilizedAmount,
                    Description: $"Allocation {allocation.AllocationId}: utilized amount " +
                                 $"{allocation.UtilizedAmount} exceeds allocated amount {allocation.AllocatedAmount}"));
            }

            var reservation = reservations.FirstOrDefault(r => r.ReservationId == allocation.ReservationId);
            if (reservation is not null)
            {
                var totalAllocatedForReservation = poolAllocations
                    .Where(a => a.ReservationId == reservation.ReservationId)
                    .Sum(a => a.AllocatedAmount);

                if (totalAllocatedForReservation > reservation.ReservedAmount)
                {
                    discrepancies.Add(new DiscrepancyRecord(
                        Type: DiscrepancyType.AllocationOverflow,
                        ReferenceId: reservation.ReservationId,
                        ExpectedValue: reservation.ReservedAmount,
                        ActualValue: totalAllocatedForReservation,
                        Description: $"Reservation {reservation.ReservationId}: total allocations " +
                                     $"{totalAllocatedForReservation} exceed reserved amount {reservation.ReservedAmount}"));
                }
            }
        }

        return poolAllocations.Count;
    }

    private static int ReconcileDistributions(
        RunCapitalReconciliationCommand command,
        IReadOnlyList<DistributionRecord> distributions,
        List<DiscrepancyRecord> discrepancies)
    {
        var poolDistributions = distributions
            .Where(d => d.PoolId == command.PoolId)
            .ToList();

        if (command.InvestorIdentityId.HasValue)
        {
            poolDistributions = poolDistributions
                .Where(d => d.InvestorIdentityId == command.InvestorIdentityId.Value)
                .ToList();
        }

        var groupedByInvestor = poolDistributions
            .GroupBy(d => d.InvestorIdentityId)
            .ToList();

        var totalOwnership = groupedByInvestor
            .Select(g => g.First().OwnershipPercentage)
            .Sum();

        if (groupedByInvestor.Count > 0 && Math.Abs(totalOwnership - 100m) > 0.01m
            && !command.InvestorIdentityId.HasValue)
        {
            discrepancies.Add(new DiscrepancyRecord(
                Type: DiscrepancyType.DistributionInconsistency,
                ReferenceId: command.PoolId,
                ExpectedValue: 100m,
                ActualValue: totalOwnership,
                Description: $"Pool {command.PoolId}: total ownership percentage {totalOwnership}% " +
                             $"does not equal 100%"));
        }

        return poolDistributions.Count;
    }

    private static int ReconcileContributionsAgainstLedger(
        IReadOnlyList<ContributionRecord> contributions,
        IReadOnlyList<LedgerEntryRecord> ledgerEntries,
        List<DiscrepancyRecord> discrepancies)
    {
        var recordsChecked = 0;

        foreach (var contribution in contributions)
        {
            recordsChecked++;

            var matchingEntry = ledgerEntries.FirstOrDefault(
                e => e.ReferenceId == contribution.ContributionId
                     && e.EntryType == "Contribution");

            if (matchingEntry is null)
            {
                discrepancies.Add(new DiscrepancyRecord(
                    Type: DiscrepancyType.BalanceMismatch,
                    ReferenceId: contribution.ContributionId,
                    ExpectedValue: contribution.Amount,
                    ActualValue: 0m,
                    Description: $"Contribution {contribution.ContributionId}: no matching ledger entry found"));
                continue;
            }

            if (matchingEntry.Amount != contribution.Amount)
            {
                discrepancies.Add(new DiscrepancyRecord(
                    Type: DiscrepancyType.BalanceMismatch,
                    ReferenceId: contribution.ContributionId,
                    ExpectedValue: contribution.Amount,
                    ActualValue: matchingEntry.Amount,
                    Description: $"Contribution {contribution.ContributionId}: amount {contribution.Amount} " +
                                 $"does not match ledger entry amount {matchingEntry.Amount}"));
            }
        }

        return recordsChecked;
    }
}
