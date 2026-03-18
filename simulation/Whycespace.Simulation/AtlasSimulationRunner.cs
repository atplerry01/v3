
namespace Whycespace.Simulation;

using System.Collections.Concurrent;
using Whycespace.Shared.Envelopes;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Whycespace.Engines.T3I.Projections.Implementations;
using Whycespace.Engines.T3I.Projections.Models;
using Whycespace.Engines.T3I.Projections.Pipeline;
using Whycespace.Engines.T3I.Projections.Registry;
using Whycespace.Engines.T3I.Projections.Stores;
using Whycespace.Contracts.Events;

/// <summary>
/// Integrates the T3I Atlas intelligence layer with the simulation engine.
/// Wires up projections, projection registry, orchestrator, and EventIngestionService
/// to process simulated events through the full intelligence pipeline.
/// </summary>
public sealed class AtlasSimulationRunner
{
    private readonly AtlasProjectionRegistry _projectionRegistry;
    private readonly IntelligenceOrchestrator _orchestrator;
    private readonly EventIngestionService _ingestionService;

    // Projection stores — exposed for assertions and metrics
    public AtlasProjectionStore<CapitalBalanceModel> CapitalBalanceStore { get; }
    public AtlasProjectionStore<VaultCashflowModel> VaultCashflowStore { get; }
    public AtlasProjectionStore<RevenueAggregationModel> RevenueStore { get; }
    public AtlasProjectionStore<IdentityGraphModel> IdentityStore { get; }
    public AtlasProjectionStore<WorkforcePerformanceModel> WorkforceStore { get; }

    private long _totalEventsIngested;
    private long _totalProjectionsApplied;
    private long _totalEngineExecutions;
    private long _totalEngineSuccesses;
    private long _totalEngineFailures;
    private readonly ConcurrentBag<double> _ingestionLatenciesMs = new();

    public AtlasSimulationRunner(ILoggerFactory? loggerFactory = null)
    {
        var factory = loggerFactory ?? NullLoggerFactory.Instance;

        // Create projection stores
        CapitalBalanceStore = new AtlasProjectionStore<CapitalBalanceModel>();
        VaultCashflowStore = new AtlasProjectionStore<VaultCashflowModel>();
        RevenueStore = new AtlasProjectionStore<RevenueAggregationModel>();
        IdentityStore = new AtlasProjectionStore<IdentityGraphModel>();
        WorkforceStore = new AtlasProjectionStore<WorkforcePerformanceModel>();

        // Create projections
        var capitalProjection = new CapitalBalanceProjection(CapitalBalanceStore);
        var cashflowProjection = new VaultCashflowProjection(VaultCashflowStore);
        var revenueProjection = new RevenueAggregationProjection(RevenueStore);
        var identityProjection = new IdentityGraphProjection(IdentityStore);
        var workforceProjection = new WorkforcePerformanceProjection(WorkforceStore);

        // Register projections
        _projectionRegistry = new AtlasProjectionRegistry();
        _projectionRegistry.Register(capitalProjection);
        _projectionRegistry.Register(cashflowProjection);
        _projectionRegistry.Register(revenueProjection);
        _projectionRegistry.Register(identityProjection);
        _projectionRegistry.Register(workforceProjection);

        // Create orchestrator (no pipeline steps registered — pure projection mode)
        _orchestrator = new IntelligenceOrchestrator(factory.CreateLogger<IntelligenceOrchestrator>());

        // Wire ingestion service
        _ingestionService = new EventIngestionService(
            _projectionRegistry,
            _orchestrator,
            factory.CreateLogger<EventIngestionService>());
    }

    /// <summary>
    /// Registers a pipeline step to bind an engine to the orchestrator for event-driven execution.
    /// </summary>
    public void RegisterPipelineStep(IIntelligencePipelineStep step) =>
        _orchestrator.Register(step);

    /// <summary>
    /// Ingests a single simulated event through the full pipeline:
    /// projection application → engine orchestration → result collection.
    /// </summary>
    public async Task<IngestionResult> IngestAsync(EventEnvelope envelope)
    {
        var sw = Stopwatch.StartNew();
        var result = await _ingestionService.IngestAsync(envelope);
        sw.Stop();

        Interlocked.Increment(ref _totalEventsIngested);
        _ingestionLatenciesMs.Add(sw.Elapsed.TotalMilliseconds);

        if (result.ProjectionApplied)
            Interlocked.Increment(ref _totalProjectionsApplied);

        foreach (var engineResult in result.EngineResults)
        {
            Interlocked.Increment(ref _totalEngineExecutions);
            if (engineResult.Success)
                Interlocked.Increment(ref _totalEngineSuccesses);
            else
                Interlocked.Increment(ref _totalEngineFailures);
        }

        return result;
    }

    /// <summary>
    /// Ingests a batch of events sequentially, returning all results.
    /// </summary>
    public async Task<IReadOnlyList<IngestionResult>> IngestBatchAsync(IEnumerable<EventEnvelope> events)
    {
        var results = new List<IngestionResult>();
        foreach (var envelope in events)
        {
            results.Add(await IngestAsync(envelope));
        }
        return results;
    }

    /// <summary>
    /// Runs a full simulation cycle: generates random events and ingests them.
    /// </summary>
    public async Task<AtlasSimulationReport> RunAsync(int eventCount, CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();
        var results = new List<IngestionResult>();

        for (var i = 0; i < eventCount && !ct.IsCancellationRequested; i++)
        {
            var envelope = SimulationEventGenerator.GenerateRandom();
            results.Add(await IngestAsync(envelope));
        }

        sw.Stop();

        return new AtlasSimulationReport(
            TotalEvents: eventCount,
            TotalIngested: Interlocked.Read(ref _totalEventsIngested),
            TotalProjectionsApplied: Interlocked.Read(ref _totalProjectionsApplied),
            TotalEngineExecutions: Interlocked.Read(ref _totalEngineExecutions),
            TotalEngineSuccesses: Interlocked.Read(ref _totalEngineSuccesses),
            TotalEngineFailures: Interlocked.Read(ref _totalEngineFailures),
            Elapsed: sw.Elapsed,
            AverageIngestionLatencyMs: _ingestionLatenciesMs.IsEmpty ? 0 : _ingestionLatenciesMs.Average(),
            CapitalBalanceCount: CapitalBalanceStore.Count,
            CashflowCount: VaultCashflowStore.Count,
            RevenueCount: RevenueStore.Count,
            IdentityCount: IdentityStore.Count,
            WorkforceCount: WorkforceStore.Count,
            Results: results);
    }
}

public sealed record AtlasSimulationReport(
    int TotalEvents,
    long TotalIngested,
    long TotalProjectionsApplied,
    long TotalEngineExecutions,
    long TotalEngineSuccesses,
    long TotalEngineFailures,
    TimeSpan Elapsed,
    double AverageIngestionLatencyMs,
    int CapitalBalanceCount,
    int CashflowCount,
    int RevenueCount,
    int IdentityCount,
    int WorkforceCount,
    IReadOnlyList<IngestionResult> Results)
{
    public bool AllSucceeded => Results.All(r => r.AllEnginesSucceeded || r.EngineResults.Count == 0);

    public void PrintSummary()
    {
        Console.WriteLine();
        Console.WriteLine("┌─────────────────────────────────────────┐");
        Console.WriteLine("│     Atlas Simulation Report             │");
        Console.WriteLine("├─────────────────────────────────────────┤");
        Console.WriteLine($"│ Events ingested:      {TotalIngested,16:N0} │");
        Console.WriteLine($"│ Projections applied:  {TotalProjectionsApplied,16:N0} │");
        Console.WriteLine($"│ Engine executions:    {TotalEngineExecutions,16:N0} │");
        Console.WriteLine($"│ Engine successes:     {TotalEngineSuccesses,16:N0} │");
        Console.WriteLine($"│ Engine failures:      {TotalEngineFailures,16:N0} │");
        Console.WriteLine($"│ Avg ingestion (ms):   {AverageIngestionLatencyMs,16:F3} │");
        Console.WriteLine($"│ Total elapsed:        {Elapsed.TotalSeconds,13:F2} s │");
        Console.WriteLine("├─────────────────────────────────────────┤");
        Console.WriteLine($"│ Capital balance SPVs: {CapitalBalanceCount,16:N0} │");
        Console.WriteLine($"│ Cashflow SPVs:        {CashflowCount,16:N0} │");
        Console.WriteLine($"│ Revenue SPVs:         {RevenueCount,16:N0} │");
        Console.WriteLine($"│ Identity records:     {IdentityCount,16:N0} │");
        Console.WriteLine($"│ Workforce records:    {WorkforceCount,16:N0} │");
        Console.WriteLine("└─────────────────────────────────────────┘");
    }
}
