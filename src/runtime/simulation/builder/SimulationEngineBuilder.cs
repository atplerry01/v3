namespace Whycespace.SimulationRuntime.Builder;

using Whycespace.SimulationRuntime.Engine;
using Whycespace.SimulationRuntime.Policy;

public sealed class SimulationEngineBuilder
{
    private SimulationPolicy? _policy;

    public SimulationEngineBuilder WithPolicy(SimulationPolicy policy)
    {
        _policy = policy;
        return this;
    }

    public SimulationEngine Build()
    {
        return new SimulationEngine(_policy ?? new SimulationPolicy());
    }
}
