namespace Whycespace.RuntimeValidation.Reports;

public sealed record ValidationReport(
    Guid ScenarioId,
    string ScenarioName,
    bool Success,
    TimeSpan ExecutionTime,
    IReadOnlyList<string> Steps,
    string? Errors
);
