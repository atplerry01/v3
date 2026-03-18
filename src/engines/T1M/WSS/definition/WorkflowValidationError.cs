namespace Whycespace.Engines.T1M.WSS.Definition;

public sealed record WorkflowValidationError(
    string Code,
    string Message,
    string Component,
    string? StepId = null
);
