namespace Whycespace.Engines.T1M.WSS.Validation;

public sealed record WorkflowValidationError(
    string Code,
    string Message,
    string Component,
    string? StepId = null
);
