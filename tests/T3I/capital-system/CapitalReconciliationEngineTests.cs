namespace Whycespace.CapitalSystem.Tests;

using Whycespace.Engines.T3I.Reporting.Economic.Engines;
using Whycespace.Engines.T3I.Reporting.Economic.Models;

public sealed class CapitalReconciliationEngineTests
{
    private readonly CapitalReconciliationEngine _engine = new();

    private static RunCapitalReconciliationCommand CreateCommand(
        Guid? poolId = null,
        Guid? investorIdentityId = null,
        ReconciliationScope scope = ReconciliationScope.FullSystem)
    {
        return new RunCapitalReconciliationCommand(
            PoolId: poolId ?? Guid.NewGuid(),
            InvestorIdentityId: investorIdentityId,
            Scope: scope,
            RequestedBy: Guid.NewGuid(),
            RequestedAt: DateTime.UtcNow);
    }

    [Fact]
    public void PoolBalanceMismatchDetection_DetectsContributionMismatch()
    {
        var poolId = Guid.NewGuid();
        var command = CreateCommand(poolId: poolId, scope: ReconciliationScope.PoolLevel);

        var contributions = new List<ContributionRecord>
        {
            new(Guid.NewGuid(), poolId, Guid.NewGuid(), 50_000m, "GBP", DateTime.UtcNow),
            new(Guid.NewGuid(), poolId, Guid.NewGuid(), 30_000m, "GBP", DateTime.UtcNow)
        };

        var poolSummary = new PoolBalanceSummary(
            PoolId: poolId,
            TotalContributions: 90_000m, // mismatch: actual sum is 80_000
            TotalReserved: 0m,
            TotalAllocated: 0m,
            TotalUtilized: 0m,
            TotalDistributed: 0m,
            ReportedBalance: 90_000m);

        var report = _engine.Reconcile(
            command, poolSummary, contributions,
            Array.Empty<LedgerEntryRecord>(),
            Array.Empty<ReservationRecord>(),
            Array.Empty<AllocationRecord>(),
            Array.Empty<DistributionRecord>());

        Assert.True(report.TotalDiscrepancies > 0);
        Assert.Contains(report.Discrepancies,
            d => d.Type == DiscrepancyType.BalanceMismatch);
    }

    [Fact]
    public void ReservationIntegrityValidation_DetectsAllocationOverflow()
    {
        var poolId = Guid.NewGuid();
        var reservationId = Guid.NewGuid();
        var command = CreateCommand(poolId: poolId, scope: ReconciliationScope.PoolLevel);

        var reservations = new List<ReservationRecord>
        {
            new(reservationId, poolId, ReservedAmount: 10_000m, AllocatedAmount: 15_000m, IsActive: true)
        };

        var poolSummary = new PoolBalanceSummary(
            poolId, 100_000m, 10_000m, 15_000m, 0m, 0m, 90_000m);

        var report = _engine.Reconcile(
            command, poolSummary,
            new List<ContributionRecord> { new(Guid.NewGuid(), poolId, Guid.NewGuid(), 100_000m, "GBP", DateTime.UtcNow) },
            Array.Empty<LedgerEntryRecord>(),
            reservations,
            Array.Empty<AllocationRecord>(),
            Array.Empty<DistributionRecord>());

        Assert.Contains(report.Discrepancies,
            d => d.Type == DiscrepancyType.AllocationOverflow
                 && d.ReferenceId == reservationId);
    }

    [Fact]
    public void AllocationConsistencyValidation_DetectsUtilizationExceedsAllocation()
    {
        var poolId = Guid.NewGuid();
        var allocationId = Guid.NewGuid();
        var reservationId = Guid.NewGuid();
        var command = CreateCommand(poolId: poolId, scope: ReconciliationScope.AllocationLevel);

        var reservations = new List<ReservationRecord>
        {
            new(reservationId, poolId, 50_000m, 20_000m, true)
        };

        var allocations = new List<AllocationRecord>
        {
            new(allocationId, reservationId, poolId,
                AllocatedAmount: 20_000m, UtilizedAmount: 25_000m, IsActive: true)
        };

        var report = _engine.Reconcile(
            command, null,
            Array.Empty<ContributionRecord>(),
            Array.Empty<LedgerEntryRecord>(),
            reservations,
            allocations,
            Array.Empty<DistributionRecord>());

        Assert.Contains(report.Discrepancies,
            d => d.Type == DiscrepancyType.UtilizationExceedsAllocation
                 && d.ReferenceId == allocationId);
    }

    [Fact]
    public void DistributionConsistencyValidation_DetectsOwnershipImbalance()
    {
        var poolId = Guid.NewGuid();
        var command = CreateCommand(poolId: poolId, scope: ReconciliationScope.InvestorLevel);

        var distributions = new List<DistributionRecord>
        {
            new(Guid.NewGuid(), poolId, Guid.NewGuid(), 5_000m, 40m, DateTime.UtcNow),
            new(Guid.NewGuid(), poolId, Guid.NewGuid(), 3_000m, 30m, DateTime.UtcNow)
            // total ownership = 70%, not 100%
        };

        var report = _engine.Reconcile(
            command, null,
            Array.Empty<ContributionRecord>(),
            Array.Empty<LedgerEntryRecord>(),
            Array.Empty<ReservationRecord>(),
            Array.Empty<AllocationRecord>(),
            distributions);

        Assert.Contains(report.Discrepancies,
            d => d.Type == DiscrepancyType.DistributionInconsistency);
    }

    [Fact]
    public async Task ConcurrentReconciliationRequests_ProduceDeterministicResults()
    {
        var poolId = Guid.NewGuid();
        var contributionId = Guid.NewGuid();
        var command = CreateCommand(poolId: poolId, scope: ReconciliationScope.FullSystem);

        var contributions = new List<ContributionRecord>
        {
            new(contributionId, poolId, Guid.NewGuid(), 50_000m, "GBP", DateTime.UtcNow)
        };

        var ledgerEntries = new List<LedgerEntryRecord>
        {
            new(Guid.NewGuid(), poolId, "Contribution", 50_000m, contributionId, DateTime.UtcNow)
        };

        var poolSummary = new PoolBalanceSummary(poolId, 50_000m, 0m, 0m, 0m, 0m, 50_000m);

        var tasks = Enumerable.Range(0, 100)
            .Select(_ => Task.Run(() => _engine.Reconcile(
                command, poolSummary, contributions, ledgerEntries,
                Array.Empty<ReservationRecord>(),
                Array.Empty<AllocationRecord>(),
                Array.Empty<DistributionRecord>())))
            .ToArray();

        var results = await Task.WhenAll(tasks);

        Assert.All(results, r =>
        {
            Assert.Equal(0, r.TotalDiscrepancies);
            Assert.Empty(r.Discrepancies);
        });
    }

    [Fact]
    public void FullSystemReconciliation_DetectsContributionLedgerMismatch()
    {
        var poolId = Guid.NewGuid();
        var contributionId = Guid.NewGuid();
        var command = CreateCommand(poolId: poolId, scope: ReconciliationScope.FullSystem);

        var contributions = new List<ContributionRecord>
        {
            new(contributionId, poolId, Guid.NewGuid(), 25_000m, "GBP", DateTime.UtcNow)
        };

        var ledgerEntries = new List<LedgerEntryRecord>
        {
            new(Guid.NewGuid(), poolId, "Contribution", 20_000m, contributionId, DateTime.UtcNow)
        };

        var poolSummary = new PoolBalanceSummary(poolId, 25_000m, 0m, 0m, 0m, 0m, 25_000m);

        var report = _engine.Reconcile(
            command, poolSummary, contributions, ledgerEntries,
            Array.Empty<ReservationRecord>(),
            Array.Empty<AllocationRecord>(),
            Array.Empty<DistributionRecord>());

        Assert.Contains(report.Discrepancies,
            d => d.Type == DiscrepancyType.BalanceMismatch
                 && d.ReferenceId == contributionId);
    }

    [Fact]
    public void CleanReconciliation_ReturnsNoDiscrepancies()
    {
        var poolId = Guid.NewGuid();
        var contributionId = Guid.NewGuid();
        var command = CreateCommand(poolId: poolId, scope: ReconciliationScope.FullSystem);

        var contributions = new List<ContributionRecord>
        {
            new(contributionId, poolId, Guid.NewGuid(), 100_000m, "GBP", DateTime.UtcNow)
        };

        var ledgerEntries = new List<LedgerEntryRecord>
        {
            new(Guid.NewGuid(), poolId, "Contribution", 100_000m, contributionId, DateTime.UtcNow)
        };

        var reservations = new List<ReservationRecord>
        {
            new(Guid.NewGuid(), poolId, 50_000m, 30_000m, true)
        };

        var poolSummary = new PoolBalanceSummary(poolId, 100_000m, 50_000m, 30_000m, 0m, 0m, 50_000m);

        var distributions = new List<DistributionRecord>
        {
            new(Guid.NewGuid(), poolId, Guid.NewGuid(), 5_000m, 60m, DateTime.UtcNow),
            new(Guid.NewGuid(), poolId, Guid.NewGuid(), 3_000m, 40m, DateTime.UtcNow)
        };

        var report = _engine.Reconcile(
            command, poolSummary, contributions, ledgerEntries,
            reservations, Array.Empty<AllocationRecord>(), distributions);

        Assert.Equal(0, report.TotalDiscrepancies);
        Assert.Empty(report.Discrepancies);
        Assert.Equal(poolId, report.PoolId);
        Assert.Equal(ReconciliationScope.FullSystem, report.Scope);
    }

    [Fact]
    public void ReconciliationReport_IsImmutable()
    {
        var poolId = Guid.NewGuid();
        var command = CreateCommand(poolId: poolId, scope: ReconciliationScope.PoolLevel);

        var poolSummary = new PoolBalanceSummary(poolId, 10_000m, 0m, 0m, 0m, 0m, 10_000m);
        var contributions = new List<ContributionRecord>
        {
            new(Guid.NewGuid(), poolId, Guid.NewGuid(), 10_000m, "GBP", DateTime.UtcNow)
        };

        var report = _engine.Reconcile(
            command, poolSummary, contributions,
            Array.Empty<LedgerEntryRecord>(),
            Array.Empty<ReservationRecord>(),
            Array.Empty<AllocationRecord>(),
            Array.Empty<DistributionRecord>());

        Assert.IsAssignableFrom<IReadOnlyList<DiscrepancyRecord>>(report.Discrepancies);
    }
}
