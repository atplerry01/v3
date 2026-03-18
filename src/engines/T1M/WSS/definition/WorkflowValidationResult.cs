namespace Whycespace.Engines.T1M.WSS.Definition;

public sealed class WorkflowValidationResult
{
    public string? WorkflowId { get; }

    public bool IsValid => Errors.Count == 0;

    public string ValidationStatus => IsValid ? "Valid" : "Invalid";

    public IReadOnlyList<WorkflowValidationError> Errors { get; }

    public IReadOnlyList<WorkflowValidationError> Warnings { get; }

    public DateTimeOffset ValidatedAt { get; }

    private WorkflowValidationResult(
        string? workflowId,
        IReadOnlyList<WorkflowValidationError> errors,
        IReadOnlyList<WorkflowValidationError> warnings,
        DateTimeOffset validatedAt)
    {
        WorkflowId = workflowId;
        Errors = errors;
        Warnings = warnings;
        ValidatedAt = validatedAt;
    }

    public static WorkflowValidationResult Valid(string? workflowId = null) =>
        new(workflowId, Array.Empty<WorkflowValidationError>(), Array.Empty<WorkflowValidationError>(), DateTimeOffset.UtcNow);

    public static WorkflowValidationResult Invalid(
        IReadOnlyList<WorkflowValidationError> errors,
        string? workflowId = null) =>
        new(workflowId, errors, Array.Empty<WorkflowValidationError>(), DateTimeOffset.UtcNow);

    public static WorkflowValidationResult Create(
        IReadOnlyList<WorkflowValidationError> errors,
        IReadOnlyList<WorkflowValidationError> warnings,
        string? workflowId = null) =>
        new(workflowId, errors, warnings, DateTimeOffset.UtcNow);

    public static WorkflowValidationResult Combine(params WorkflowValidationResult[] results)
    {
        var errors = new List<WorkflowValidationError>();
        var warnings = new List<WorkflowValidationError>();
        string? workflowId = null;

        foreach (var result in results)
        {
            errors.AddRange(result.Errors);
            warnings.AddRange(result.Warnings);
            workflowId ??= result.WorkflowId;
        }

        return new WorkflowValidationResult(workflowId, errors, warnings, DateTimeOffset.UtcNow);
    }
}
