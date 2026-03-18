namespace Whycespace.AtlasSimulation.Tests;

using Whycespace.Platform.Simulation.Runner;
using Whycespace.Platform.Simulation.EventCapture;

public sealed class AtlasSimulationRunnerTests
{
    [Fact]
    public async Task IngestCapitalContribution_UpdatesCapitalBalanceProjection()
    {
        var runner = new AtlasSimulationRunner();
        var spvId = Guid.NewGuid();

        var envelope = SimulationEventGenerator.GenerateCapitalContribution(spvId, 10_000m);
        var result = await runner.IngestAsync(envelope);

        Assert.True(result.ProjectionApplied);
        var balance = runner.CapitalBalanceStore.Get(spvId);
        Assert.NotNull(balance);
        Assert.Equal(10_000m, balance.TotalContributions);
        Assert.Equal(10_000m, balance.NetBalance);
        Assert.Equal(1, balance.TransactionCount);
    }

    [Fact]
    public async Task IngestCapitalDistribution_ReducesNetBalance()
    {
        var runner = new AtlasSimulationRunner();
        var poolId = Guid.NewGuid();

        // First contribute
        await runner.IngestAsync(SimulationEventGenerator.GenerateCapitalContribution(poolId, 50_000m));
        // Then distribute
        await runner.IngestAsync(SimulationEventGenerator.GenerateCapitalDistribution(poolId, 10_000m));

        var balance = runner.CapitalBalanceStore.Get(poolId);
        Assert.NotNull(balance);
        Assert.Equal(50_000m, balance.TotalContributions);
        Assert.Equal(10_000m, balance.TotalDistributions);
        Assert.Equal(40_000m, balance.NetBalance);
        Assert.Equal(2, balance.TransactionCount);
    }

    [Fact]
    public async Task IngestRevenueAndProfit_UpdatesCashflowAndRevenueProjections()
    {
        var runner = new AtlasSimulationRunner();
        var spvId = Guid.NewGuid();

        await runner.IngestAsync(SimulationEventGenerator.GenerateRevenueRecorded(spvId, 5_000m));
        await runner.IngestAsync(SimulationEventGenerator.GenerateProfitDistributed(spvId, 1_000m));

        // Cashflow projection
        var cashflow = runner.VaultCashflowStore.Get(spvId);
        Assert.NotNull(cashflow);
        Assert.Equal(5_000m, cashflow.TotalInflows);
        Assert.Equal(1_000m, cashflow.TotalOutflows);
        Assert.Equal(4_000m, cashflow.NetCashflow);

        // Revenue projection
        var revenue = runner.RevenueStore.Get(spvId);
        Assert.NotNull(revenue);
        Assert.Equal(5_000m, revenue.TotalRevenue);
        Assert.Equal(1_000m, revenue.TotalProfitDistributed);
        Assert.Equal(4_000m, revenue.UndistributedRevenue);
    }

    [Fact]
    public async Task IngestIdentityEvents_BuildsIdentityGraph()
    {
        var runner = new AtlasSimulationRunner();
        var identityId = Guid.NewGuid();

        await runner.IngestAsync(SimulationEventGenerator.GenerateIdentityRegistered(identityId));
        await runner.IngestAsync(SimulationEventGenerator.GenerateIdentityActivated(identityId));
        await runner.IngestAsync(SimulationEventGenerator.GenerateIdentityRoleAssigned(identityId, "Driver"));

        var identity = runner.IdentityStore.Get(identityId);
        Assert.NotNull(identity);
        Assert.Equal("Active", identity.Status);
        Assert.Contains("Driver", identity.Roles);
    }

    [Fact]
    public async Task IngestWorkforceEvents_TracksPerformance()
    {
        var runner = new AtlasSimulationRunner();
        var workerId = Guid.NewGuid();

        await runner.IngestAsync(SimulationEventGenerator.GenerateTaskAssigned(workerId));
        await runner.IngestAsync(SimulationEventGenerator.GenerateTaskAssigned(workerId));
        await runner.IngestAsync(SimulationEventGenerator.GenerateTaskCompleted(workerId));

        var performance = runner.WorkforceStore.Get(workerId);
        Assert.NotNull(performance);
        Assert.Equal(2, performance.TasksAssigned);
        Assert.Equal(1, performance.TasksCompleted);
        Assert.Equal(0.5, performance.CompletionRate, 0.01);
    }

    [Fact]
    public async Task IngestBatch_ProcessesAllEvents()
    {
        var runner = new AtlasSimulationRunner();
        var spvId = Guid.NewGuid();

        var events = SimulationEventGenerator.GenerateEconomicLifecycle(spvId, 10);
        var results = await runner.IngestBatchAsync(events);

        Assert.Equal(10, results.Count);
        Assert.True(results.All(r => r.ProjectionApplied));
    }

    [Fact]
    public async Task EventIdempotency_DuplicateEventIgnored()
    {
        var runner = new AtlasSimulationRunner();
        var spvId = Guid.NewGuid();

        var envelope = SimulationEventGenerator.GenerateCapitalContribution(spvId, 10_000m);

        await runner.IngestAsync(envelope);
        await runner.IngestAsync(envelope); // duplicate

        var balance = runner.CapitalBalanceStore.Get(spvId);
        Assert.NotNull(balance);
        Assert.Equal(10_000m, balance.TotalContributions); // not doubled
        Assert.Equal(1, balance.TransactionCount);
    }

    [Fact]
    public async Task RunFullSimulation_ProducesReport()
    {
        var runner = new AtlasSimulationRunner();
        var report = await runner.RunAsync(50);

        Assert.Equal(50, report.TotalEvents);
        Assert.Equal(50, report.TotalIngested);
        Assert.True(report.TotalProjectionsApplied > 0);
        Assert.True(report.AverageIngestionLatencyMs >= 0);
        Assert.True(report.Elapsed.TotalMilliseconds > 0);
        Assert.True(report.AllSucceeded);
    }

    [Fact]
    public async Task RunFullSimulation_PopulatesProjectionStores()
    {
        var runner = new AtlasSimulationRunner();
        await runner.RunAsync(100);

        // With 100 random events, we should have some records in at least some stores
        var totalRecords = runner.CapitalBalanceStore.Count
            + runner.VaultCashflowStore.Count
            + runner.RevenueStore.Count
            + runner.IdentityStore.Count
            + runner.WorkforceStore.Count;

        Assert.True(totalRecords > 0, "Expected at least some projection records from 100 random events");
    }

    [Fact]
    public async Task MultipleSpvs_IsolatedProjections()
    {
        var runner = new AtlasSimulationRunner();
        var spv1 = Guid.NewGuid();
        var spv2 = Guid.NewGuid();

        await runner.IngestAsync(SimulationEventGenerator.GenerateCapitalContribution(spv1, 10_000m));
        await runner.IngestAsync(SimulationEventGenerator.GenerateCapitalContribution(spv2, 20_000m));

        var balance1 = runner.CapitalBalanceStore.Get(spv1);
        var balance2 = runner.CapitalBalanceStore.Get(spv2);

        Assert.NotNull(balance1);
        Assert.NotNull(balance2);
        Assert.Equal(10_000m, balance1.NetBalance);
        Assert.Equal(20_000m, balance2.NetBalance);
    }
}
