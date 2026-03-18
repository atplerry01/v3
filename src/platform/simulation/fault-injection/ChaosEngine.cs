namespace Whycespace.Platform.Simulation.FaultInjection;

using Whycespace.Contracts.Engines;
using Whycespace.Shared.Envelopes;
using Whycespace.Platform.Simulation.Metrics;
using Whycespace.Platform.Simulation.Trace;

/// <summary>
/// Enterprise-grade chaos engineering engine for simulation.
/// Supports multiple fault strategies: latency injection, engine failure,
/// partial failure, and timeout simulation.
/// All faults are simulated — no real infrastructure is affected.
/// </summary>
public sealed class ChaosEngine
{
    private readonly double _faultRate;
    private readonly SimulationMetrics _metrics;
    private readonly SimulationTraceCollector _traceCollector;
    private readonly ThreadLocal<Random> _rng = new(() => new Random(Environment.TickCount ^ Thread.CurrentThread.ManagedThreadId));
    private readonly List<ChaosRule> _rules = new();

    private long _totalFaultsInjected;
    private long _latencyFaults;
    private long _failureFaults;
    private long _timeoutFaults;

    public ChaosEngine(double faultRate, SimulationMetrics metrics, SimulationTraceCollector traceCollector)
    {
        _faultRate = faultRate;
        _metrics = metrics;
        _traceCollector = traceCollector;
    }

    /// <summary>
    /// Adds a targeted chaos rule for a specific engine.
    /// </summary>
    public void AddRule(ChaosRule rule) => _rules.Add(rule);

    /// <summary>
    /// Evaluates whether a fault should be injected for this invocation
    /// and returns the fault type if so.
    /// </summary>
    public ChaosDecision Evaluate(Guid simulationId, EngineInvocationEnvelope envelope)
    {
        var r = _rng.Value!;

        // Check targeted rules first
        foreach (var rule in _rules)
        {
            if (rule.EngineName == envelope.EngineName && r.NextDouble() < rule.FaultRate)
            {
                _traceCollector.Record(simulationId, "ChaosRuleTriggered",
                    $"Chaos rule '{rule.Name}' triggered for engine '{envelope.EngineName}': {rule.FaultType}");
                RecordFault(rule.FaultType);
                return new ChaosDecision(ShouldFault: true, FaultType: rule.FaultType, Rule: rule);
            }
        }

        // Global fault rate
        if (_faultRate > 0 && r.NextDouble() < _faultRate)
        {
            var faultType = (ChaosRuleFaultType)(r.Next(3));
            _traceCollector.Record(simulationId, "ChaosFaultInjected",
                $"Global chaos fault injected for engine '{envelope.EngineName}': {faultType}");
            RecordFault(faultType);
            return new ChaosDecision(ShouldFault: true, FaultType: faultType, Rule: null);
        }

        return ChaosDecision.NoFault;
    }

    /// <summary>
    /// Applies the chaos decision to produce a faulted EngineResult.
    /// </summary>
    public async Task<EngineResult> ApplyFaultAsync(ChaosDecision decision)
    {
        return decision.FaultType switch
        {
            ChaosRuleFaultType.Latency => await ApplyLatencyFault(decision),
            ChaosRuleFaultType.Failure => EngineResult.Fail("ChaosEngine: Simulated engine failure"),
            ChaosRuleFaultType.Timeout => EngineResult.Fail("ChaosEngine: Simulated timeout"),
            _ => EngineResult.Fail("ChaosEngine: Unknown fault type")
        };
    }

    private async Task<EngineResult> ApplyLatencyFault(ChaosDecision decision)
    {
        var delayMs = decision.Rule?.LatencyMs ?? _rng.Value!.Next(100, 2000);
        await Task.Delay(delayMs);
        return EngineResult.Fail($"ChaosEngine: Simulated latency fault ({delayMs}ms)");
    }

    private void RecordFault(ChaosRuleFaultType faultType)
    {
        Interlocked.Increment(ref _totalFaultsInjected);
        switch (faultType)
        {
            case ChaosRuleFaultType.Latency:
                Interlocked.Increment(ref _latencyFaults);
                break;
            case ChaosRuleFaultType.Failure:
                Interlocked.Increment(ref _failureFaults);
                break;
            case ChaosRuleFaultType.Timeout:
                Interlocked.Increment(ref _timeoutFaults);
                break;
        }
        _metrics.RecordDeadLetter();
    }

    public long TotalFaultsInjected => Interlocked.Read(ref _totalFaultsInjected);
    public long LatencyFaults => Interlocked.Read(ref _latencyFaults);
    public long FailureFaults => Interlocked.Read(ref _failureFaults);
    public long TimeoutFaults => Interlocked.Read(ref _timeoutFaults);
}

public sealed record ChaosRule(
    string Name,
    string EngineName,
    ChaosRuleFaultType FaultType,
    double FaultRate,
    int LatencyMs = 500);

public enum ChaosRuleFaultType
{
    Latency = 0,
    Failure = 1,
    Timeout = 2
}

public sealed record ChaosDecision(
    bool ShouldFault,
    ChaosRuleFaultType FaultType,
    ChaosRule? Rule)
{
    public static readonly ChaosDecision NoFault = new(false, default, null);
}
