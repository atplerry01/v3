namespace Whycespace.Engines.T1M.WSS.Validation;

public sealed class WorkflowValidationResult
{
    public bool IsValid => Errors.Count == 0;

    public IReadOnlyList<WorkflowValidationError> Errors { get; }

    public IReadOnlyList<WorkflowValidationError> Warnings { get; }

    private WorkflowValidationResult(
        IReadOnlyList<WorkflowValidationError> errors,
        IReadOnlyList<WorkflowValidationError> warnings)
    {
        Errors = errors;
        Warnings = warnings;
    }

    public static WorkflowValidationResult Valid() =>
        new(Array.Empty<WorkflowValidationError>(), Array.Empty<WorkflowValidationError>());

    public static WorkflowValidationResult Invalid(IReadOnlyList<WorkflowValidationError> errors) =>
        new(errors, Array.Empty<WorkflowValidationError>());

    public static WorkflowValidationResult Create(
        IReadOnlyList<WorkflowValidationError> errors,
        IReadOnlyList<WorkflowValidationError> warnings) =>
        new(errors, warnings);

    public static WorkflowValidationResult Combine(params WorkflowValidationResult[] results)
    {
        var errors = new List<WorkflowValidationError>();
        var warnings = new List<WorkflowValidationError>();

        foreach (var result in results)
        {
            errors.AddRange(result.Errors);
            warnings.AddRange(result.Warnings);
        }

        return new WorkflowValidationResult(errors, warnings);
    }
}
