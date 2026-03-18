namespace Whycespace.Simulation;

using global::System.Diagnostics;
using Whycespace.Engines.T0U.WhycePolicy.Validation.Engines;
using Whycespace.Engines.T2E;
using Whycespace.Engines.T2E.Clusters.Mobility.Taxi.Engines;
using Whycespace.Engines.T2E.Clusters.Property.Letting.Engines;
using Whycespace.Engines.T3I.Atlas.Workforce.Engines;
using Whycespace.Engines.T3I.Atlas.Workforce.Models;
using Whycespace.Runtime.Dispatcher;
using Whycespace.EngineRuntime.Registry;
using Whycespace.Runtime.Reliability;
using Whycespace.WorkflowRuntime;
using Whycespace.Contracts.Engines;
using Whycespace.Contracts.Workflows;

public sealed class SimulationRunner
{
    private readonly SimulationConfig _config;
    private readonly SimulationMetrics _metrics;
    private readonly EngineRegistry _registry;
    private readonly DeadLetterQueue _dlq;
    private readonly FaultInjector _faultInjector;
    private readonly RetryPolicyEngine _retryPolicy;
    private readonly AtlasSimulationRunner? _atlasRunner;

    public SimulationRunner(SimulationConfig config, SimulationMetrics metrics, AtlasSimulationRunner? atlasRunner = null)
    {
        _config = config;
        _metrics = metrics;
        _atlasRunner = atlasRunner;
        _dlq = new DeadLetterQueue();
        _retryPolicy = new RetryPolicyEngine
        {
            MaxRetries = 2,
            InitialDelay = TimeSpan.FromMilliseconds(1)
        };
        _faultInjector = new FaultInjector(config.FaultRate, _dlq, metrics);

        _registry = new EngineRegistry();
        _registry.Register(new PolicyValidationEngine());
        _registry.Register(new DriverMatchingEngine());
        _registry.Register(new TenantMatchingEngine());
        _registry.Register(new WorkforceAssignmentEngine());
        _registry.Register(new RideExecutionEngine());
        _registry.Register(new PropertyExecutionEngine());
    }

    public async Task RunAsync(CancellationToken ct = default)
    {
        Console.WriteLine($"[Simulation] Scenario: {_config.Name}");
        Console.WriteLine($"[Simulation] Workflows: {_config.TotalWorkflows:N0}");
        Console.WriteLine($"[Simulation] Workers: {_config.Workers}");
        Console.WriteLine($"[Simulation] Fault rate: {_config.FaultRate:P1}");
        Console.WriteLine();

        _metrics.Start();

        var semaphore = new SemaphoreSlim(_config.Workers, _config.Workers);
        var tasks = new List<Task>();
        var deadline = _config.Duration.HasValue
            ? DateTimeOffset.UtcNow + _config.Duration.Value
            : DateTimeOffset.MaxValue;

        var progressInterval = Math.Max(1, _config.TotalWorkflows / 20);

        for (var i = 0; i < _config.TotalWorkflows; i++)
        {
            if (ct.IsCancellationRequested) break;
            if (DateTimeOffset.UtcNow >= deadline) break;

            await semaphore.WaitAsync(ct);

            var index = i;
            tasks.Add(Task.Run(async () =>
            {
                try
                {
                    await ExecuteSingleWorkflowAsync();

                    if (index % progressInterval == 0 && index > 0)
                    {
                        var pct = (double)index / _config.TotalWorkflows * 100;
                        Console.Write($"\r[Simulation] Progress: {pct:F0}% ({index:N0}/{_config.TotalWorkflows:N0})  ");
                    }
                }
                finally
                {
                    semaphore.Release();
                }
            }, ct));
        }

        await Task.WhenAll(tasks);
        _metrics.Stop();

        Console.WriteLine();
        Console.WriteLine($"[Simulation] Completed in {_metrics.Elapsed.TotalSeconds:F2}s");
        Console.WriteLine($"[Simulation] Dead letters: {_dlq.Count}");
        Console.WriteLine($"[Simulation] Faults injected: {_faultInjector.FaultsInjected}");
    }

    private async Task ExecuteSingleWorkflowAsync()
    {
        var payload = WorkloadGenerator.GenerateRandom();
        var sw = Stopwatch.StartNew();

        var dispatcher = new RuntimeDispatcher(_registry);
        var stateStore = new WorkflowStateStore();

        var context = new Dictionary<string, object>(payload.Context);
        var graph = payload.Graph;
        var workflowId = graph.WorkflowId;

        var state = new WorkflowState(
            workflowId, graph.Steps[0].StepId, WorkflowStatus.Running,
            context, DateTimeOffset.UtcNow, null);
        stateStore.Save(state);

        var success = true;

        foreach (var step in graph.Steps)
        {
            var engineSw = Stopwatch.StartNew();

            var envelope = new EngineInvocationEnvelope(
                Guid.NewGuid(), step.EngineName, workflowId,
                step.StepId, workflowId, context);

            EngineResult result;
            if (_faultInjector.ShouldInjectFault())
            {
                _faultInjector.InjectEngineFault(envelope);
                result = EngineResult.Fail("Simulated fault");
            }
            else
            {
                result = await dispatcher.DispatchAsync(envelope);
            }

            engineSw.Stop();
            _metrics.RecordEngineInvocation(step.EngineName, engineSw.Elapsed.TotalMilliseconds);

            if (!result.Success)
            {
                success = false;
                break;
            }

            foreach (var ev in result.Events)
                _metrics.RecordEventPublished();

            foreach (var kvp in result.Output)
                context[kvp.Key] = kvp.Value;

            // Apply events to Atlas intelligence pipeline if wired
            if (_atlasRunner is not null)
            {
                var projSw = Stopwatch.StartNew();
                foreach (var ev in result.Events)
                {
                    var simEvent = SimulationEventGenerator.GenerateRandom();
                    await _atlasRunner.IngestAsync(simEvent);
                }
                // Also generate a context-appropriate event for the workflow type
                var contextEvent = GenerateWorkflowEvent(payload.WorkflowType, context);
                if (contextEvent is not null)
                    await _atlasRunner.IngestAsync(contextEvent);
                projSw.Stop();
                _metrics.RecordProjectionUpdate(projSw.Elapsed.TotalMilliseconds);
            }
            else
            {
                var projSw = Stopwatch.StartNew();
                projSw.Stop();
                _metrics.RecordProjectionUpdate(projSw.Elapsed.TotalMilliseconds);
            }
        }

        sw.Stop();
        _metrics.RecordWorkflowCompleted(payload.WorkflowType, sw.Elapsed.TotalMilliseconds, success);
    }

    private static EventFabric.Models.EventEnvelope? GenerateWorkflowEvent(
        string workflowType, Dictionary<string, object> context)
    {
        return workflowType switch
        {
            "EconomicLifecycle" =>
                context.TryGetValue("spvName", out var spvName)
                    ? SimulationEventGenerator.GenerateCapitalContribution()
                    : null,
            "RideRequest" =>
                SimulationEventGenerator.GenerateTaskAssigned(),
            "PropertyListing" =>
                SimulationEventGenerator.GenerateRevenueRecorded(),
            _ => null
        };
    }
}
