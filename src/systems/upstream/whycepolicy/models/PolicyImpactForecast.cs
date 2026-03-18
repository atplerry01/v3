namespace Whycespace.Systems.Upstream.WhycePolicy.Models;

public sealed record PolicyImpactForecast(
    string PolicyId,
    string Domain,
    int SimulatedContexts,
    int AllowedCount,
    int DeniedCount,
    int LoggedCount,
    DateTime GeneratedAt
);
