namespace Whycespace.Platform.Simulation.FaultInjection;

using Whycespace.Shared.Envelopes;
using Whycespace.Runtime.Reliability;
using Whycespace.Contracts.Engines;
using Whycespace.Contracts.Runtime;
using Whycespace.Platform.Simulation.Metrics;

public sealed class FaultInjector
{
    private readonly double _faultRate;
    private readonly DeadLetterQueue _dlq;
    private readonly SimulationMetrics _metrics;
    private readonly ThreadLocal<Random> _rng = new(() => new Random(Environment.TickCount ^ Thread.CurrentThread.ManagedThreadId));

    public long FaultsInjected => _faultsInjected;
    private long _faultsInjected;

    public FaultInjector(double faultRate, DeadLetterQueue dlq, SimulationMetrics metrics)
    {
        _faultRate = faultRate;
        _dlq = dlq;
        _metrics = metrics;
    }

    public bool ShouldInjectFault()
    {
        if (_faultRate <= 0) return false;
        return _rng.Value!.NextDouble() < _faultRate;
    }

    public void InjectEngineFault(EngineInvocationEnvelope envelope)
    {
        Interlocked.Increment(ref _faultsInjected);
        _dlq.Enqueue(envelope, "Simulated engine fault");
        _metrics.RecordDeadLetter();
    }
}

public sealed class FaultAwareDispatcher
{
    private readonly IEngineRuntimeDispatcher _inner;
    private readonly FaultInjector _injector;
    private readonly RetryPolicyEngine _retryPolicy;
    private readonly SimulationMetrics _metrics;

    public FaultAwareDispatcher(
        IEngineRuntimeDispatcher inner,
        FaultInjector injector,
        RetryPolicyEngine retryPolicy,
        SimulationMetrics metrics)
    {
        _inner = inner;
        _injector = injector;
        _retryPolicy = retryPolicy;
        _metrics = metrics;
    }

    public async Task<EngineResult> DispatchAsync(EngineInvocationEnvelope envelope)
    {
        if (_injector.ShouldInjectFault())
        {
            _injector.InjectEngineFault(envelope);
            return EngineResult.Fail("Simulated fault injected");
        }

        return await _retryPolicy.ExecuteWithRetryAsync(
            async () =>
            {
                var result = await _inner.DispatchAsync(envelope);
                if (result.Success)
                {
                    foreach (var _ in result.Events)
                        _metrics.RecordEventPublished();
                }
                return result;
            },
            result =>
            {
                if (!result.Success)
                {
                    _metrics.RecordRetry();
                    return true;
                }
                return false;
            });
    }
}