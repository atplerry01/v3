namespace Whycespace.Runtime.Validation.Models;

public sealed record ValidationScenario(
    Guid ScenarioId,
    string ScenarioName,
    string ClusterName,
    string Description
);
