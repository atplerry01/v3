namespace Whycespace.Systems.Downstream.Coordination.Simulation;

public interface ISimulationAware
{
    Task<SimulationResult> SimulateAsync(SimulationContext context);
}
