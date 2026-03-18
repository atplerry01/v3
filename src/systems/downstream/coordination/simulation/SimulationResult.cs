namespace Whycespace.Systems.Downstream.Coordination.Simulation;

public sealed record SimulationResult(
    bool WouldSucceed,
    string OperationType,
    IReadOnlyList<string> Steps,
    IReadOnlyList<string> PolicyDecisions,
    IReadOnlyList<string> EventsToEmit,
    string? FailureReason = null
)
{
    public static SimulationResult Success(string operationType, IReadOnlyList<string> steps, IReadOnlyList<string> policies, IReadOnlyList<string> events) =>
        new(true, operationType, steps, policies, events);

    public static SimulationResult Failure(string operationType, string reason, IReadOnlyList<string> steps, IReadOnlyList<string> policies) =>
        new(false, operationType, steps, policies, [], reason);
}
