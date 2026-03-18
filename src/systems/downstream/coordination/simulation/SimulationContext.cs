namespace Whycespace.Systems.Downstream.Coordination.Simulation;

public sealed record SimulationContext(
    SimulationMode Mode,
    string CorrelationId,
    DateTimeOffset Timestamp,
    IReadOnlyDictionary<string, object>? Parameters = null
)
{
    public bool IsDryRun => Mode == SimulationMode.DryRun;
    public bool IsLive => Mode == SimulationMode.Live;

    public static SimulationContext Live(string correlationId) => new(
        SimulationMode.Live,
        correlationId,
        DateTimeOffset.UtcNow
    );

    public static SimulationContext DryRun(string correlationId, IReadOnlyDictionary<string, object>? parameters = null) => new(
        SimulationMode.DryRun,
        correlationId,
        DateTimeOffset.UtcNow,
        parameters
    );
}
