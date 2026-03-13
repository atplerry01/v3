namespace Whycespace.RuntimeValidation.Models;

public sealed record ValidationScenario(
    Guid ScenarioId,
    string ScenarioName,
    string ClusterName,
    string Description
);
