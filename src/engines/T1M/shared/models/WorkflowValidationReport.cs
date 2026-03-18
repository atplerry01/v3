namespace Whycespace.Engines.T1M.Shared;

/// <summary>
/// Domain model representing the result of workflow validation.
/// </summary>
public sealed record WorkflowValidationReport(
    string WorkflowId,
    ValidationStatus ValidationStatus,
    IReadOnlyList<string> Errors,
    IReadOnlyList<string> Warnings,
    DateTimeOffset ValidatedAt
);

public enum ValidationStatus
{
    Valid,
    Invalid
}
